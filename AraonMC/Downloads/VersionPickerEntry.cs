using AraonMC.Core.Domain.Entities;

namespace AraonMC.Downloads;

/// <summary>Entry in the version picker ComboBox — either a group header or a clickable version item.</summary>
public sealed record VersionPickerEntry
{
    /// <summary>Display text shown to the user (group name or version id).</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>The Minecraft version string (e.g. "1.20.1"), null for group headers.</summary>
    public string? Version { get; init; }

    /// <summary>Version type for the badge color (Release, Snapshot, OldBeta, OldAlpha).</summary>
    public VersionType Type { get; init; }

    /// <summary>True if this entry is a non-selectable group header.</summary>
    public bool IsGroup { get; init; }

    /// <summary>Release date, used for sorting.</summary>
    public DateTimeOffset ReleaseTime { get; init; }

    /// <summary>Short label for the version type badge.</summary>
    public string TypeLabel => Type switch
    {
        VersionType.Release => "release",
        VersionType.Snapshot => "snapshot",
        VersionType.OldBeta => "beta",
        VersionType.OldAlpha => "alpha",
        _ => "release",
    };
}
