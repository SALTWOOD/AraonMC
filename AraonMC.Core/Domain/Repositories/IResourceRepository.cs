using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Enums;

namespace AraonMC.Core.Domain.Repositories;

/// <summary>
/// Remote content catalog (repository contract). Backed by the merging
/// <c>Infrastructure.Catalog.ResourceRepository</c>, which fans a <see cref="ResourceSearchQuery"/> out to
/// Modrinth and/or CurseForge.
/// </summary>
public interface IResourceRepository
{
    Task<IReadOnlyList<ResourceInfo>> SearchAsync(ResourceSearchQuery query, CancellationToken ct = default);

    /// <summary>Whether a CurseForge API key was supplied (else CurseForge searches return nothing).</summary>
    bool IsCurseForgeConfigured { get; }

    /// <summary>
    /// Lists the downloadable versions of <paramref name="resource"/> (newest first), each carrying its
    /// supported game versions and primary file URL/filename. Used by the version-select dialog.
    /// </summary>
    Task<IReadOnlyList<ResourceVersion>> GetVersionsAsync(ResourceInfo resource, CancellationToken ct = default);
}

/// <summary>Search ordering shared by both platforms; each client maps it to its native sort field.</summary>
public enum ResourceSort
{
    Relevance,
    Downloads,
    Updated,
    Newest,
}

/// <summary>
/// Selects which catalogs to query. <see cref="All"/> merges results from both platforms.
/// </summary>
[Flags]
public enum ResourceSourceFilter
{
    None = 0,
    Modrinth = 1,
    CurseForge = 2,
    All = Modrinth | CurseForge,
}

/// <summary>Parameters for <see cref="IResourceRepository.SearchAsync"/>.</summary>
public sealed class ResourceSearchQuery
{
    public required ResourceType Type { get; init; } = ResourceType.Mod;

    /// <summary>Free-text search filter (matched against name/author/summary by the platforms). Empty = browse all.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>Optional loader filter (Fabric/Forge/Quilt/NeoForge). <c>null</c> = any loader.</summary>
    public LoaderType? Loader { get; init; }

    /// <summary>Optional game version string, e.g. "1.20.1". <c>null</c> = any version.</summary>
    public string? GameVersion { get; init; }

    public ResourceSort Sort { get; init; } = ResourceSort.Relevance;

    public ResourceSourceFilter Sources { get; init; } = ResourceSourceFilter.All;

    /// <summary>Max results requested per platform (each client caps it to its own page-size maximum).</summary>
    public int Limit { get; init; } = 30;

    /// <summary>Result offset for pagination (0-based).</summary>
    public int Offset { get; init; } = 0;

    /// <summary>Set by the repository after a search completes with the total matching count across all sources.</summary>
    public int TotalCount { get; set; }
}
