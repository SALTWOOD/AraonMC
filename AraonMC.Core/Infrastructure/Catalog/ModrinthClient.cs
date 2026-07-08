using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Enums;
using AraonMC.Core.Domain.Repositories;

namespace AraonMC.Core.Infrastructure.Catalog;

/// <summary>
/// Modrinth Labrinth v2 catalog adapter. Search is anonymous (no token). Modrinth asks for a descriptive
/// <c>User-Agent</c> on every request, so one is sent per request. Rate limit is 300 req/min per IP.
/// Loaders are filtered as <c>categories</c> facets (there is no separate loader facet on v2).
/// </summary>
public sealed class ModrinthClient
{
    private const string BaseUrl = "https://api.modrinth.com/v2";
    private const string UserAgent = "AraonMC/0.1.0 (https://github.com/SALTWOOD/AraonMC)";

    private static readonly HashSet<string> LoaderCategories = new(StringComparer.Ordinal)
    {
        "fabric", "forge", "neoforge", "quilt", "liteloader", "babric", "legacy-fabric",
    };

    private readonly HttpClient _http;

    public ModrinthClient(HttpClient http) => _http = http;

    public bool IsConfigured => true;

    public async Task<IReadOnlyList<ResourceInfo>> SearchAsync(ResourceSearchQuery query, CancellationToken ct = default)
    {
        var projectType = CatalogMappings.ModrinthProjectType(query.Type);
        if (projectType is null)
        {
            DebugLog.Info($"Modrinth: resource type '{query.Type}' is not a Modrinth project type; skipping.");
            return [];
        }

        // facets = AND across inner arrays; each constraint is its own single-element OR group.
        var facets = new List<List<string>> { new() { $"project_type:{projectType}" } };
        if (CatalogMappings.ModrinthLoaderCategory(query.Loader ?? LoaderType.Vanilla) is { } loader)
            facets.Add(new List<string> { $"categories:{loader}" });
        if (!string.IsNullOrWhiteSpace(query.GameVersion))
            facets.Add(new List<string> { $"versions:{query.GameVersion!.Trim()}" });

        var limit = Math.Clamp(query.Limit, 1, 100);
        var offset = Math.Max(query.Offset, 0);
        var url = BuildSearchUrl(query.Text, facets, query.Sort, limit, offset);
        DebugLog.Info($"Modrinth: GET {url}");

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.UserAgent.TryParseAdd(UserAgent); // best-effort; Modrinth tolerates a missing UA.
        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<SearchResponse>(cancellationToken: ct).ConfigureAwait(false);
        if (body?.Hits is null || body.Hits.Count == 0)
        {
            DebugLog.Info($"Modrinth: no results for type '{query.Type}', text='{query.Text}'.");
            return [];
        }

        query.TotalCount = Math.Max(query.TotalCount, body.TotalHits);

        var results = new List<ResourceInfo>(body.Hits.Count);
        foreach (var hit in body.Hits) results.Add(Map(hit, query.Type));
        DebugLog.Info($"Modrinth: returned {results.Count} result(s) for type '{query.Type}' (total: {body.TotalHits}).");
        return results;
    }

    /// <summary>
    /// Lists the project's downloadable versions (newest-first from the API), each with its primary file
    /// URL/filename, supported game versions and loaders. Versions with no downloadable file are skipped.
    /// </summary>
    public async Task<IReadOnlyList<ResourceVersion>> GetVersionsAsync(string projectKey, CancellationToken ct = default)
    {
        var key = string.IsNullOrWhiteSpace(projectKey) ? "" : Uri.EscapeDataString(projectKey);
        var url = $"{BaseUrl}/project/{key}/version";
        DebugLog.Info($"Modrinth: listing versions — GET {url}");

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.UserAgent.TryParseAdd(UserAgent);
        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var versions = await resp.Content.ReadFromJsonAsync<List<VersionEntry>>(cancellationToken: ct).ConfigureAwait(false);
        if (versions is null || versions.Count == 0) return Array.Empty<ResourceVersion>();

        var results = new List<ResourceVersion>(versions.Count);
        foreach (var v in versions)
        {
            var file = v.Files?.FirstOrDefault(f => f.Primary == true) ?? v.Files?.FirstOrDefault();
            if (file is null || string.IsNullOrWhiteSpace(file.Url)) continue; // no downloadable file
            results.Add(new ResourceVersion
            {
                Id = v.Id ?? string.Empty,
                Name = v.Name ?? v.VersionNumber ?? string.Empty,
                ReleaseType = NormalizeReleaseType(v.VersionType),
                PublishedAt = TryParseDate(v.DatePublished),
                Downloads = v.Downloads,
                DownloadUrl = file.Url!,
                FileName = file.Filename ?? string.Empty,
                SizeBytes = file.Size,
                GameVersions = v.GameVersions ?? (IReadOnlyList<string>)Array.Empty<string>(),
                Loaders = v.Loaders ?? (IReadOnlyList<string>)Array.Empty<string>(),
            });
        }
        DebugLog.Info($"Modrinth: listed {results.Count} downloadable version(s) for '{projectKey}'.");
        return results;
    }

    private static string NormalizeReleaseType(string? t) => t switch
    {
        "beta" => "beta",
        "alpha" => "alpha",
        _ => "release",
    };

    private static string BuildSearchUrl(string text, List<List<string>> facets, ResourceSort sort, int limit, int offset)
    {
        var facetsJson = JsonSerializer.Serialize(facets);
        var parts = new List<string>
        {
            $"limit={limit.ToString(CultureInfo.InvariantCulture)}",
            $"offset={offset.ToString(CultureInfo.InvariantCulture)}",
            $"index={CatalogMappings.ModrinthIndex(sort)}",
            $"facets={Uri.EscapeDataString(facetsJson)}",
        };
        if (!string.IsNullOrWhiteSpace(text))
            parts.Add($"query={Uri.EscapeDataString(text.Trim())}");
        return $"{BaseUrl}/search?{string.Join('&', parts)}";
    }

    private static ResourceInfo Map(SearchHit hit, ResourceType type) => new()
    {
        Id = hit.ProjectId ?? string.Empty,
        Slug = hit.Slug ?? string.Empty,
        Name = hit.Title ?? string.Empty,
        Author = hit.Author ?? string.Empty,
        Summary = hit.Description ?? string.Empty,
        Category = PickDisplayCategory(hit.Categories),
        Downloads = hit.Downloads,
        IconKey = CatalogMappings.Initials(hit.Title ?? string.Empty),
        IconUrl = hit.IconUrl ?? string.Empty,
        PageUrl = BuildPageUrl(hit.ProjectType ?? "mod", hit.Slug ?? string.Empty),
        UpdatedAt = TryParseDate(hit.DateModified),
        Type = type,
        Source = ResourceSource.Modrinth,
    };

    /// <summary>Picks the first non-loader category for display (loaders like "fabric" are not useful labels).</summary>
    private static string PickDisplayCategory(List<string>? categories)
    {
        if (categories is null || categories.Count == 0) return string.Empty;
        foreach (var c in categories)
            if (!string.IsNullOrEmpty(c) && !LoaderCategories.Contains(c))
                return TitleCase(c);
        return TitleCase(categories[0]);
    }

    private static string TitleCase(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];

    private static string BuildPageUrl(string projectType, string slug) =>
        $"https://modrinth.com/{projectType}/{slug}";

    private static DateTimeOffset? TryParseDate(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso)) return null;
        return DateTimeOffset.TryParse(iso, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto) ? dto : null;
    }

    // ---- Minimal response DTOs (only the fields we consume; exact casing via attributes). ----

    private sealed class SearchResponse
    {
        [JsonPropertyName("hits")] public List<SearchHit>? Hits { get; set; }
        [JsonPropertyName("total_hits")] public int TotalHits { get; set; }
    }

    private sealed class SearchHit
    {
        [JsonPropertyName("project_id")] public string? ProjectId { get; set; }
        [JsonPropertyName("project_type")] public string? ProjectType { get; set; }
        [JsonPropertyName("slug")] public string? Slug { get; set; }
        [JsonPropertyName("author")] public string? Author { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("categories")] public List<string>? Categories { get; set; }
        [JsonPropertyName("downloads")] public long Downloads { get; set; }
        [JsonPropertyName("icon_url")] public string? IconUrl { get; set; }
        [JsonPropertyName("date_modified")] public string? DateModified { get; set; }
    }

    // /project/{id}/version response (array; newest-first).
    private sealed class VersionEntry
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("version_number")] public string? VersionNumber { get; set; }
        [JsonPropertyName("version_type")] public string? VersionType { get; set; }
        [JsonPropertyName("downloads")] public long Downloads { get; set; }
        [JsonPropertyName("date_published")] public string? DatePublished { get; set; }
        [JsonPropertyName("game_versions")] public List<string>? GameVersions { get; set; }
        [JsonPropertyName("loaders")] public List<string>? Loaders { get; set; }
        [JsonPropertyName("files")] public List<VersionFile>? Files { get; set; }
    }

    private sealed class VersionFile
    {
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("filename")] public string? Filename { get; set; }
        [JsonPropertyName("primary")] public bool? Primary { get; set; }
        [JsonPropertyName("size")] public long Size { get; set; }
    }
}
