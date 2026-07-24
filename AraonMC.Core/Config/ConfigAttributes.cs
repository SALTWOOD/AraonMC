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

using System;

namespace AraonMC.Core.Config;

/// <summary>Marks the <c>Config</c> partial class as the root config catalog that the
/// source generator turns into a typed, observable facade.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ConfigCatalogAttribute : Attribute;

/// <summary>Marks a nested partial class of <c>Config</c> as a config section.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class SectionAttribute : Attribute
{
    /// <summary>Which scope the keys in this section belong to.</summary>
    public ConfigScope Scope { get; set; } = ConfigScope.Global;

    /// <summary>
    /// TOML table path for this section (e.g. <c>general</c>, <c>java</c>).
    /// Each key's full path is <c>{Section.Path}.{key}</c>.
    /// </summary>
    public string Path { get; set; } = "";
}

/// <summary>Marks a partial property as a config key.</summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class KeyAttribute : Attribute
{
    /// <summary>Override the TOML key name (defaults to snake_case of the property name).</summary>
    public string? Name { get; set; }

    /// <summary>Default value used when the key is absent or fails to parse. Must be a constant.</summary>
    public object? Default { get; set; }
}
