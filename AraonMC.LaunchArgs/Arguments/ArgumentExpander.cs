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

namespace AraonMC.LaunchArgs.Arguments;

/// <summary>把参数模板（含条件规则）展平为字符串列表，并处理旧式 minecraftArguments。</summary>
public sealed class ArgumentExpander
{
    private readonly RuleEvaluator _rules;

    public ArgumentExpander(RuleEvaluator rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _rules = rules;
    }

    /// <summary>展平新格式参数条目，跳过规则未命中的项。</summary>
    public IReadOnlyList<string> Expand(IReadOnlyList<VersionArgumentEntry> entries, IReadOnlySet<string> activeFeatures)
    {
        var result = new List<string>();
        foreach (var e in entries)
        {
            if (!_rules.Applies(e.Rules, activeFeatures)) continue;
            result.AddRange(e.Values);
        }
        return result;
    }

    /// <summary>旧式 minecraftArguments 按空白拆分为 token（占位符保留）。</summary>
    public IReadOnlyList<string> ExpandLegacy(string minecraftArguments)
        => minecraftArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>版本缺少 arguments.jvm 时使用的默认 JVM 参数。</summary>
    public IReadOnlyList<VersionArgumentEntry> DefaultJvmArguments()
    {
        return
        [
            new VersionArgumentEntry
            {
                Rules = [ new Rule { Action = RuleAction.Allow, Os = new OsCondition { Name = OperatingSystemKind.OSX } } ],
                Values = ["-XstartOnFirstThread"],
            },
            new VersionArgumentEntry
            {
                Rules = [ new Rule { Action = RuleAction.Allow, Os = new OsCondition { Name = OperatingSystemKind.Windows } } ],
                Values = ["-XX:HeapDumpPath=MojangTracingIntel"],
            },
            new VersionArgumentEntry
            {
                Rules = [ new Rule { Action = RuleAction.Allow, Os = new OsCondition { Name = OperatingSystemKind.Windows, Version = @"^10\." } } ],
                Values = ["-Dos.name=Windows 10", "-Dos.version=10.0"],
            },
            new VersionArgumentEntry { Values = ["-Djava.library.path=${natives_directory}"] },
            new VersionArgumentEntry { Values = ["-Dminecraft.launcher.brand=${launcher_name}"] },
            new VersionArgumentEntry { Values = ["-Dminecraft.launcher.version=${launcher_version}"] },
            new VersionArgumentEntry { Values = ["-cp", "${classpath}"] },
        ];
    }
}
