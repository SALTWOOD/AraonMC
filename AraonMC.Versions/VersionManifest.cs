namespace AraonMC.Versions;

/// <summary>Mojang version_manifest_v2.json 的解析结果。</summary>
public sealed class VersionManifest
{
    public string LatestRelease { get; set; } = string.Empty;
    public string LatestSnapshot { get; set; } = string.Empty;
    public IReadOnlyList<Entry> Versions { get; set; } = [];

    /// <summary>清单中的一个版本条目。</summary>
    public sealed class Entry
    {
        public required string Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public required Uri Url { get; set; }
        public DateTimeOffset ReleaseTime { get; set; }
    }
}
