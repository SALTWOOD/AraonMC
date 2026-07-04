using AraonMC.LaunchArgs.Rules;

namespace AraonMC.LaunchArgs.Version;

/// <summary>client.json <c>libraries</c> 中的一项（一个 Maven 制品）。</summary>
public sealed class VersionLibrary
{
    public string Name { get; set; } = string.Empty; // group:artifact:version

    public IReadOnlyList<Rule>? Rules { get; set; }

    public VersionArtifact? Artifact { get; set; }

    public IReadOnlyDictionary<OperatingSystemKind, string>? Natives { get; set; }

    public IReadOnlyDictionary<string, VersionArtifact>? Classifiers { get; set; }

    public VersionLibraryExtract? Extract { get; set; }
}

public sealed class VersionArtifact
{
    public string Path { get; set; } = string.Empty; // 相对 libraries 根
    public string Sha1 { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Url { get; set; } = string.Empty;
}

public sealed class VersionLibraryExtract
{
    public IReadOnlyList<string> Exclude { get; set; } = [];
}
