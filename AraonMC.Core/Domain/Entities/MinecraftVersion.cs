namespace AraonMC.Core.Domain.Entities;

public enum VersionType
{
    Release,
    Snapshot,
    OldBeta,
    OldAlpha,
}

/// <summary>
/// A Minecraft release manifest entry.
/// </summary>
public sealed class MinecraftVersion
{
    public string Id { get; set; } = string.Empty;
    public VersionType Type { get; set; } = VersionType.Release;
    public DateTimeOffset ReleaseTime { get; set; }
}
