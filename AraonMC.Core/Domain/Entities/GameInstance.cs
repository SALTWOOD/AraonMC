using AraonMC.Core.Domain.Enums;

namespace AraonMC.Core.Domain.Entities;

/// <summary>
/// A locally installed (or installable) game instance / profile.
/// </summary>
public sealed class GameInstance
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MinecraftVersion { get; set; } = string.Empty;
    public LoaderType Loader { get; set; } = LoaderType.Vanilla;
    public string LoaderVersion { get; set; } = string.Empty;
    public string? CoverKey { get; set; }
    public string? Group { get; set; }
    public DateTimeOffset LastPlayed { get; set; }
    public TimeSpan PlayTime { get; set; }

    public string DisplayLoader =>
        Loader == LoaderType.Vanilla ? "Vanilla" : $"{Loader} {LoaderVersion}";
}
