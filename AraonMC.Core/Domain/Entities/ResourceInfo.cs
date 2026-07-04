using AraonMC.Core.Domain.Enums;

namespace AraonMC.Core.Domain.Entities;

/// <summary>
/// A single browsable content entry (mod, modpack, resource pack, shader pack, world save, or datapack)
/// shown on the Browse page. Aggregated from one or more remote catalogs (<see cref="Source"/>).
/// </summary>
public sealed class ResourceInfo
{
    /// <summary>Platform-internal id of the project (Modrinth project id or CurseForge mod id), as a string.</summary>
    public string Id { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;

    /// <summary>A short human-readable category label (e.g. "Performance", "World Gen &amp; Mobs").</summary>
    public string Category { get; set; } = string.Empty;

    public long Downloads { get; set; }

    /// <summary>2-letter initials derived from <see cref="Name"/>; shown when <see cref="IconUrl"/> fails to load.</summary>
    public string IconKey { get; set; } = string.Empty;

    /// <summary>Remote icon URL (project logo). Empty when the project has no icon.</summary>
    public string IconUrl { get; set; } = string.Empty;

    /// <summary>Web URL of the project page on its origin platform.</summary>
    public string PageUrl { get; set; } = string.Empty;

    public DateTimeOffset? UpdatedAt { get; set; }

    public ResourceType Type { get; set; }

    /// <summary>Which catalog this entry came from.</summary>
    public ResourceSource Source { get; set; }
}
