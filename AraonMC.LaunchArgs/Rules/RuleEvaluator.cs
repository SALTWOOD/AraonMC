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

using System.Text.RegularExpressions;

namespace AraonMC.LaunchArgs.Rules;

/// <summary>评估一组规则在当前平台下是否成立。</summary>
public sealed class RuleEvaluator
{
    private readonly PlatformContext _platform;

    public RuleEvaluator(PlatformContext? platform = null) => _platform = platform ?? PlatformContext.Current;

    /// <summary>规则为空则恒包含；否则按顺序，命中的 Allow/Disallow 决定结果。</summary>
    public bool Applies(IReadOnlyList<Rule>? rules, IReadOnlySet<string> activeFeatures)
    {
        if (rules is null || rules.Count == 0) return true;

        var result = false;
        foreach (var rule in rules)
        {
            if (!MatchesConditions(rule, activeFeatures)) continue;
            result = rule.Action == RuleAction.Allow;
        }
        return result;
    }

    private bool MatchesConditions(Rule rule, IReadOnlySet<string> activeFeatures)
    {
        if (rule.Os is { } os && !MatchesOs(os)) return false;

        if (rule.Features is { } features)
        {
            foreach (var (key, required) in features)
                if (required && !activeFeatures.Contains(key)) return false;
        }

        return true;
    }

    private bool MatchesOs(OsCondition os)
    {
        if (os.Name is { } name && name != _platform.OperatingSystem) return false;

        if (!string.IsNullOrEmpty(os.Version))
        {
            try
            {
                if (!Regex.IsMatch(_platform.Version, os.Version)) return false;
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        if (!string.IsNullOrEmpty(os.Arch) &&
            !string.Equals(os.Arch, _platform.Arch, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}
