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

using AraonMC.LaunchArgs.Rules;
using AraonMC.LaunchArgs.Version;

namespace AraonMC.LaunchArgs.Libraries;

public sealed class ResolvedLibrary
{
    public VersionLibrary Library { get; set; } = null!;
    public string? ClasspathPath { get; set; }
    public VersionArtifact? Native { get; set; }
}

/// <summary>按平台规则筛选库，产出 classpath 路径与 natives。</summary>
public sealed class ClasspathResolver
{
    private readonly RuleEvaluator _rules;
    private readonly PlatformContext _platform;

    public ClasspathResolver(RuleEvaluator rules, PlatformContext? platform = null)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _rules = rules;
        _platform = platform ?? PlatformContext.Current;
    }

    public IReadOnlyList<ResolvedLibrary> Resolve(
        IReadOnlyList<VersionLibrary> libraries,
        IReadOnlySet<string> activeFeatures)
    {
        var result = new List<ResolvedLibrary>(libraries.Count);
        foreach (var lib in libraries)
        {
            if (!_rules.Applies(lib.Rules, activeFeatures)) continue;

            var resolved = new ResolvedLibrary { Library = lib };

            // 优先 downloads.artifact.path，缺失则按 Maven 坐标推导。
            if (lib.Artifact is { } art && !string.IsNullOrEmpty(art.Path))
                resolved.ClasspathPath = art.Path;
            else if (!string.IsNullOrEmpty(lib.Name))
                resolved.ClasspathPath = MavenPathFromName(lib.Name);

            if (lib.Natives is { } natives && natives.TryGetValue(_platform.OperatingSystem, out var classifier))
            {
                classifier = classifier.Replace("${arch}", _platform.Arch.Contains("64") ? "64" : "32");
                if (lib.Classifiers is { } classifiers && classifiers.TryGetValue(classifier, out var nativeArt))
                    resolved.Native = nativeArt;
            }

            result.Add(resolved);
        }
        return result;
    }

    private static string MavenPathFromName(string name)
    {
        var parts = name.Split(':');
        if (parts.Length < 3) return name;
        var group = parts[0].Replace('.', '/');
        var artifact = parts[1];
        var version = parts[2];
        var file = parts.Length >= 4
            ? $"{artifact}-{version}-{parts[3]}.jar"
            : $"{artifact}-{version}.jar";
        return $"{group}/{artifact}/{version}/{file}";
    }
}
