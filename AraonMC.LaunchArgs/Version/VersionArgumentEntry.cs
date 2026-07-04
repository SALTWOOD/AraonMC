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
