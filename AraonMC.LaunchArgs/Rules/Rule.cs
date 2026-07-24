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

namespace AraonMC.LaunchArgs.Rules;

public enum OperatingSystemKind
{
    Windows,
    Linux,
    OSX,
}

public enum RuleAction
{
    Allow,
    Disallow,
}

/// <summary>client.json 的条件规则，参数条目和库的平台过滤共用。</summary>
public sealed class Rule
{
    public RuleAction Action { get; set; }

    public OsCondition? Os { get; set; }

    public IReadOnlyDictionary<string, bool>? Features { get; set; }
}

public sealed class OsCondition
{
    public OperatingSystemKind? Name { get; set; }
    public string? Version { get; set; }
    public string? Arch { get; set; }
}
