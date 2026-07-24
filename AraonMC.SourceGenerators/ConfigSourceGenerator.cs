// Copyright (C) 2026 SALTWOOD and contributors
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AraonMC.SourceGenerators;

/// <summary>
/// Turns an attribute-decorated <c>[ConfigCatalog]</c> partial class into a typed, observable
/// config facade. For each <c>[Section]</c>-scoped, <c>[Key]</c>-marked partial property it emits
/// an implementation that delegates to <c>IConfigStore</c>. Global keys become INPC properties;
/// Instance keys become <c>InstanceKey&lt;T&gt;</c> indexers (<c>Config.Section.Key[instance]</c>).
/// </summary>
[Generator]
public sealed class ConfigSourceGenerator : IIncrementalGenerator
{
    private const string CatalogAttributeFullName = "AraonMC.Core.Config.ConfigCatalogAttribute";
    private const string SectionAttributeFullName = "AraonMC.Core.Config.SectionAttribute";
    private const string KeyAttributeFullName = "AraonMC.Core.Config.KeyAttribute";
    private const string InstanceKeyFullName = "AraonMC.Core.Config.InstanceKey`1";
    private const string ConfigNamespace = "AraonMC.Core.Config";

    private static readonly SymbolDisplayFormat TypeFormat = new(
        SymbolDisplayGlobalNamespaceStyle.Included,
        SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    private static string FormatType(ITypeSymbol t)
    {
        var s = t.ToDisplayString(TypeFormat);
        // ToDisplayString (4.12) drops the nullable reference-type '?' annotation; re-add it.
        if (t.NullableAnnotation == NullableAnnotation.Annotated && !s.EndsWith("?"))
            s += "?";
        return s;
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var catalogs = context.SyntaxProvider.ForAttributeWithMetadataName(
            CatalogAttributeFullName,
            predicate: (node, _) => node is ClassDeclarationSyntax c && c.AttributeLists.Count > 0,
            transform: (ctx, _) => BuildModel((INamedTypeSymbol)ctx.TargetSymbol));

        context.RegisterSourceOutput(catalogs, (spc, model) =>
        {
            if (model is null) return;
            spc.AddSource($"{model.ConfigClassName}.Generated.g.cs", Emit(model));
        });
    }

    // ---- Model ----------------------------------------------------------------

    private sealed class CatalogModel
    {
        public string Namespace = "";
        public string ConfigClassName = "";
        public string ConfigAccessibility = "";
        public List<SectionModel> Sections = new();
    }

    private sealed class SectionModel
    {
        public string ClassName = "";
        public string PropertyName = ""; // Config.<PropertyName>
        public string Accessibility = "";
        public bool IsInstance;
        public string TomlPath = "";
        public List<KeyModel> Keys = new();
    }

    private sealed class KeyModel
    {
        public string PropertyName = "";
        public string Accessibility = "";
        public string TypeDisplay = "";          // full property type
        public string InnerTypeDisplay = "";     // for InstanceKey<T>: the T
        public string TomlName = "";
        public string DefaultLiteral = "default";
        public bool IsInstance;
    }

    // ---- Gathering ------------------------------------------------------------

    private static CatalogModel? BuildModel(INamedTypeSymbol configClass)
    {
        var model = new CatalogModel
        {
            Namespace = configClass.ContainingNamespace.IsGlobalNamespace
                ? ""
                : configClass.ContainingNamespace.ToDisplayString(),
            ConfigClassName = configClass.Name,
            ConfigAccessibility = AccessibilityString(configClass.DeclaredAccessibility),
        };

        foreach (var member in configClass.GetTypeMembers())
        {
            var sectionAttr = member.GetAttributes().FirstOrDefault(IsSectionAttribute);
            if (sectionAttr is null) continue;

            var section = new SectionModel
            {
                ClassName = member.Name,
                PropertyName = StripSectionSuffix(member.Name),
                Accessibility = AccessibilityString(member.DeclaredAccessibility),
            };

            // Scope + Path from [Section] named arguments.
            var scopeIsInstance = false;
            var sectionPath = member.Name;
            foreach (var arg in sectionAttr.NamedArguments)
            {
                if (arg.Key == "Scope" && arg.Value.Value is int v)
                    scopeIsInstance = v == (int)ConfigScopeValue.Instance;
                else if (arg.Key == "Path" && arg.Value.Value is string p)
                    sectionPath = p;
            }
            section.IsInstance = scopeIsInstance;
            section.TomlPath = sectionPath;

            foreach (var prop in member.GetMembers().OfType<IPropertySymbol>())
            {
                var keyAttr = prop.GetAttributes().FirstOrDefault(IsKeyAttribute);
                if (keyAttr is null) continue;
                if (!IsPartialProperty(prop)) continue;

                var km = new KeyModel
                {
                    PropertyName = prop.Name,
                    Accessibility = AccessibilityString(prop.DeclaredAccessibility),
                    TypeDisplay = FormatType(prop.Type),
                    IsInstance = scopeIsInstance,
                };

                // For instance keys the type is InstanceKey<T>; capture the declared type verbatim
                // from source so nullable annotations on T survive (ToDisplayString drops them in 4.12).
                if (scopeIsInstance
                    && prop.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).OfType<PropertyDeclarationSyntax>().FirstOrDefault() is { } instPds)
                {
                    km.TypeDisplay = instPds.Type.ToString();
                }

                // Name + Default from [Key] named arguments.
                foreach (var arg in keyAttr.NamedArguments)
                {
                    if (arg.Key == "Name" && arg.Value.Value is string name)
                        km.TomlName = name;
                    else if (arg.Key == "Default")
                        km.DefaultLiteral = EmitDefault(arg.Value);
                }
                if (string.IsNullOrEmpty(km.TomlName))
                    km.TomlName = ToSnakeCase(prop.Name);

                section.Keys.Add(km);
            }

            model.Sections.Add(section);
        }

        return model;
    }

    private static bool IsSectionAttribute(AttributeData a)
        => a.AttributeClass?.ToDisplayString() == SectionAttributeFullName;

    private static bool IsKeyAttribute(AttributeData a)
        => a.AttributeClass?.ToDisplayString() == KeyAttributeFullName;

    private static bool IsPartialProperty(IPropertySymbol prop)
    {
        foreach (var reference in prop.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is PropertyDeclarationSyntax pds
                && pds.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return true;
            }
        }
        return false;
    }

    private enum ConfigScopeValue { Global = 0, Instance = 1 }

    // ---- Emission -------------------------------------------------------------

    private static string Emit(CatalogModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System.ComponentModel;");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(model.Namespace))
            sb.AppendLine($"namespace {model.Namespace};");
        sb.AppendLine();

        sb.AppendLine($"{model.ConfigAccessibility}partial class {model.ConfigClassName}");
        sb.AppendLine("{");

        // ---- Config facade (static store + section singletons) ----
        sb.AppendLine("    private static global::AraonMC.Core.Config.IConfigStore? s_store;");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>The active config store. Throws if <see cref=\"Initialize\"/> was not called.</summary>");
        sb.AppendLine("    public static global::AraonMC.Core.Config.IConfigStore Store");
        sb.AppendLine("    {");
        sb.AppendLine("        get");
        sb.AppendLine("        {");
        sb.AppendLine("            var s = s_store;");
        sb.AppendLine("            return s ?? throw new global::System.InvalidOperationException(");
        sb.AppendLine("                \"Config has not been initialized. Call Config.Initialize(store) at startup.\");");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>Installs the backing store. Call once at startup, before any config access.</summary>");
        sb.AppendLine("    public static void Initialize(global::AraonMC.Core.Config.IConfigStore store)");
        sb.AppendLine("        => global::System.Threading.Interlocked.Exchange(ref s_store, store);");
        sb.AppendLine();

        foreach (var section in model.Sections)
        {
            sb.AppendLine($"    private static {section.ClassName}? s_{section.PropertyName};");
            sb.AppendLine($"    /// <summary>The <c>{section.TomlPath}</c> section ({(section.IsInstance ? "per-instance" : "global")}).</summary>");
            sb.AppendLine($"    public static {section.ClassName} {section.PropertyName} => s_{section.PropertyName} ??= new {section.ClassName}();");
            sb.AppendLine();
        }

        // ---- Section class implementations ----
        foreach (var section in model.Sections)
        {
            EmitSection(sb, section);
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void EmitSection(StringBuilder sb, SectionModel section)
    {
        sb.AppendLine($"    {section.Accessibility}partial class {section.ClassName}");
        sb.AppendLine("    {");

        if (!section.IsInstance)
        {
            sb.AppendLine("        public event global::System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;");
            sb.AppendLine();
            sb.AppendLine("        private void OnPropertyChanged(string name)");
            sb.AppendLine("            => PropertyChanged?.Invoke(this, new global::System.ComponentModel.PropertyChangedEventArgs(name));");
            sb.AppendLine();
        }

        foreach (var key in section.Keys)
        {
            var fullPath = $"{section.TomlPath}.{key.TomlName}";
            var scope = section.IsInstance ? "Instance" : "Global";

            if (key.IsInstance)
            {
                // InstanceKey<T> get-only property with a cached backing accessor.
                sb.AppendLine($"        private {key.TypeDisplay}? _backing_{key.PropertyName};");
                sb.AppendLine($"        {key.Accessibility}partial {key.TypeDisplay} {key.PropertyName}");
                sb.AppendLine("        {");
                sb.AppendLine($"            get => _backing_{key.PropertyName} ??= new {key.TypeDisplay}(");
                sb.AppendLine("                global::AraonMC.Core.Config.Config.Store,");
                sb.AppendLine($"                global::AraonMC.Core.Config.ConfigScope.{scope},");
                sb.AppendLine($"                \"{fullPath}\",");
                sb.AppendLine($"                {key.DefaultLiteral});");
                sb.AppendLine("            }");
            }
            else
            {
                sb.AppendLine($"        {key.Accessibility}partial {key.TypeDisplay} {key.PropertyName}");
                sb.AppendLine("        {");
                sb.AppendLine($"            get => global::AraonMC.Core.Config.Config.Store.Get<{key.TypeDisplay}>(");
                sb.AppendLine($"                global::AraonMC.Core.Config.ConfigScope.{scope}, \"{fullPath}\", {key.DefaultLiteral});");
                sb.AppendLine("            set");
                sb.AppendLine("            {");
                sb.AppendLine($"                global::AraonMC.Core.Config.Config.Store.Set<{key.TypeDisplay}>(");
                sb.AppendLine($"                    global::AraonMC.Core.Config.ConfigScope.{scope}, \"{fullPath}\", value);");
                sb.AppendLine($"                OnPropertyChanged(nameof({key.PropertyName}));");
                sb.AppendLine("            }");
                sb.AppendLine("        }");
            }
            sb.AppendLine();
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    // ---- Helpers --------------------------------------------------------------

    private static string AccessibilityString(Accessibility a) => a switch
    {
        Accessibility.Public => "public ",
        Accessibility.Internal => "internal ",
        Accessibility.Protected => "protected ",
        Accessibility.Private => "private ",
        Accessibility.ProtectedOrInternal => "protected internal ",
        Accessibility.ProtectedAndInternal => "private protected ",
        _ => "",
    };

    private static string StripSectionSuffix(string name)
    {
        const string suffix = "Section";
        return name.EndsWith(suffix, System.StringComparison.Ordinal)
            ? name.Substring(0, name.Length - suffix.Length)
            : name;
    }

    private static string ToSnakeCase(string s)
    {
        var sb = new StringBuilder(s.Length + 4);
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (i > 0 && char.IsUpper(c))
                sb.Append('_');
            sb.Append(char.IsUpper(c) ? char.ToLowerInvariant(c) : c);
        }
        return sb.ToString();
    }

    private static string EmitDefault(TypedConstant tc)
    {
        if (tc.IsNull) return "null";
        if (tc.Kind == TypedConstantKind.Enum)
            return $"({tc.Type!.ToDisplayString(TypeFormat)}){tc.Value}";
        if (tc.Kind == TypedConstantKind.Primitive)
        {
            return tc.Value switch
            {
                string s => SymbolDisplay.FormatLiteral(s, true),
                char c => SymbolDisplay.FormatLiteral(c, true),
                bool b => b ? "true" : "false",
                _ => tc.Value?.ToString() ?? "null",
            };
        }
        return "default";
    }
}
