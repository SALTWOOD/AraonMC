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

namespace AraonMC.LaunchArgs.Version;

/// <summary>jvm/game 参数列表中的一项：纯字符串，或带规则的条件对象。</summary>
public sealed class VersionArgumentEntry
{
    /// <summary>条件规则；为 null 或空表示恒包含。</summary>
    public IReadOnlyList<Rule>? Rules { get; set; }

    /// <summary>该条目的值（纯字符串归一化为单元素数组）。</summary>
    public IReadOnlyList<string> Values { get; set; } = [];

    public bool IsConditional => Rules is { Count: > 0 };
}
