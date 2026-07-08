using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Enums;
using AraonMC.Core.Domain.Repositories;

namespace AraonMC.Core.Infrastructure.Catalog;

/// <summary>
/// CurseForge Core v1 catalog adapter. Every request needs an <c>x-api-key</c> header (the CurseForge for
/// Studios API key). When no key is configured the client reports <see cref="IsConfigured"/> = false and
/// returns no results. Minecraft <c>gameId = 432</c>. <c>modLoaderType</c> must be paired with
/// <c>gameVersion</c> or the API rejects the request, so the loader filter is omitted unless a version is set.
/// </summary>
public sealed class CurseForgeClient
{
    private const string BaseUrl = "https://api.curseforge.com/v1";
    private const int MinecraftGameId = 432;

    private readonly HttpClient _http;
    private readonly string _apiKey;

    public CurseForgeClient(HttpClient http, string apiKey)
    {
        _http = http;
        _apiKey = apiKey ?? string.Empty;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<IReadOnlyList<ResourceInfo>> SearchAsync(ResourceSearchQuery query, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            DebugLog.Info("CurseForge: API key not configured (CF_API_KEY unset); skipping search.");
            return [];
        }

        var classId = CatalogMappings.CurseForgeClassId(query.Type);
        if (classId is null)
        {
            DebugLog.Info($"CurseForge: resource type '{query.Type}' has no class mapping; skipping.");
            return [];
        }

        var pageSize = Math.Clamp(query.Limit, 1, 50);
        var index = Math.Max(query.Offset, 0);
        var url = BuildSearchUrl(query, classId.Value, pageSize, index);
        DebugLog.Info($"CurseForge: GET {url}");

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.TryAddWithoutValidation("x-api-key", _apiKey);
        req.Headers.Accept.ParseAdd("application/json");
        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<SearchResponse>(cancellationToken: ct).ConfigureAwait(false);
        if (body?.Data is null || body.Data.Count == 0)
        {
            DebugLog.Info($"CurseForge: no results for type '{query.Type}', text='{query.Text}'.");
            return [];
        }

        if (body.Pagination?.TotalCount is { } total)
            query.TotalCount = Math.Max(query.TotalCount, total);

        var results = new List<ResourceInfo>(body.Data.Count);
        foreach (var mod in body.Data) results.Add(Map(mod, query.Type));
        DebugLog.Info($"CurseForge: returned {results.Count} result(s) for type '{query.Type}'.");
        return results;
    }

    private static string BuildSearchUrl(ResourceSearchQuery q, int classId, int pageSize, int index)
    {
        var parts = new List<string>
        {
            $"gameId={MinecraftGameId.ToString(CultureInfo.InvariantCulture)}",
            $"classId={classId.ToString(CultureInfo.InvariantCulture)}",
            $"pageSize={pageSize.ToString(CultureInfo.InvariantCulture)}",
            $"index={index.ToString(CultureInfo.InvariantCulture)}",
            "sortOrder=desc",
            $"sortField={CatalogMappings.CurseForgeSortField(q.Sort).ToString(CultureInfo.InvariantCulture)}",
        };
        if (!string.IsNullOrWhiteSpace(q.Text))
            parts.Add($"searchFilter={Uri.EscapeDataString(q.Text.Trim())}");
        if (!string.IsNullOrWhiteSpace(q.GameVersion))
        {
            parts.Add($"gameVersion={Uri.EscapeDataString(q.GameVersion!.Trim())}");
            // modLoaderType is only valid alongside gameVersion on CurseForge.
            if (CatalogMappings.CurseForgeLoaderType(q.Loader ?? LoaderType.Vanilla) is { } lt)
                parts.Add($"modLoaderType={lt.ToString(CultureInfo.InvariantCulture)}");
        }
        return $"{BaseUrl}/mods/search?{string.Join('&', parts)}";
    }

    private static ResourceInfo Map(Mod mod, ResourceType type)
    {
        var category = mod.Categories is null
            ? string.Empty
            : mod.Categories.FirstOrDefault(c => c.Id == mod.PrimaryCategoryId)?.Name
              ?? mod.Categories.FirstOrDefault(c => c.IsClass != true)?.Name
              ?? mod.Categories.FirstOrDefault()?.Name
              ?? string.Empty;

        return new ResourceInfo
        {
            Id = mod.Id.ToString(CultureInfo.InvariantCulture),
            Slug = mod.Slug ?? string.Empty,
            Name = mod.Name ?? string.Empty,
            Author = mod.Authors?.FirstOrDefault()?.Name ?? string.Empty,
            Summary = mod.Summary ?? string.Empty,
            Category = category,
            Downloads = mod.DownloadCount,
            IconKey = CatalogMappings.Initials(mod.Name ?? string.Empty),
            IconUrl = mod.Logo?.ThumbnailUrl ?? string.Empty,
            PageUrl = mod.Links?.WebsiteUrl ?? string.Empty,
            UpdatedAt = TryParseDate(mod.DateModified),
            Type = type,
            Source = ResourceSource.CurseForge,
        };
    }

    /// <summary>
    /// Lists the project's downloadable files (each file is a CurseForge "version"), newest-first. Files
    /// with no <c>downloadUrl</c> (distribution blocked) are skipped. Capped at the newest 50 files.
    /// </summary>
    public async Task<IReadOnlyList<ResourceVersion>> GetFilesAsync(int modId, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            DebugLog.Info("CurseForge: not configured (CF_API_KEY unset); no files available.");
            return Array.Empty<ResourceVersion>();
        }

        var url = $"{BaseUrl}/mods/{modId.ToString(CultureInfo.InvariantCulture)}/files?pageSize=50";
        DebugLog.Info($"CurseForge: listing files — GET {url}");

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.TryAddWithoutValidation("x-api-key", _apiKey);
        req.Headers.Accept.ParseAdd("application/json");
        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<FilesResponse>(cancellationToken: ct).ConfigureAwait(false);
        if (body?.Data is null || body.Data.Count == 0)
        {
            DebugLog.Info($"CurseForge: no files for mod {modId}.");
            return Array.Empty<ResourceVersion>();
        }

        var results = new List<ResourceVersion>(body.Data.Count);
        foreach (var f in body.Data)
        {
            if (string.IsNullOrWhiteSpace(f.DownloadUrl)) continue; // distribution blocked / unavailable
            results.Add(new ResourceVersion
            {
                Id = f.Id.ToString(CultureInfo.InvariantCulture),
                Name = f.DisplayName ?? f.FileName ?? string.Empty,
                ReleaseType = f.ReleaseType switch { 2 => "beta", 3 => "alpha", _ => "release" },
                PublishedAt = TryParseDate(f.FileDate),
                DownloadUrl = f.DownloadUrl!,
                FileName = f.FileName ?? string.Empty,
                SizeBytes = f.FileLength,
                GameVersions = f.GameVersions ?? (IReadOnlyList<string>)Array.Empty<string>(),
            });
        }
        DebugLog.Info($"CurseForge: listed {results.Count} downloadable file(s) for mod {modId}.");
        return results;
    }

    private static DateTimeOffset? TryParseDate(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso)) return null;
        return DateTimeOffset.TryParse(iso, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto) ? dto : null;
    }

    // ---- Minimal response DTOs (only the fields we consume). ----

    private sealed class SearchResponse
    {
        [JsonPropertyName("data")] public List<Mod>? Data { get; set; }
        [JsonPropertyName("pagination")] public Pagination? Pagination { get; set; }
    }

    private sealed class Pagination
    {
        [JsonPropertyName("totalCount")] public int? TotalCount { get; set; }
    }

    private sealed class Mod
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("slug")] public string? Slug { get; set; }
        [JsonPropertyName("summary")] public string? Summary { get; set; }
        [JsonPropertyName("downloadCount")] public long DownloadCount { get; set; }
        [JsonPropertyName("classId")] public int? ClassId { get; set; }
        [JsonPropertyName("primaryCategoryId")] public int PrimaryCategoryId { get; set; }
        [JsonPropertyName("categories")] public List<Category>? Categories { get; set; }
        [JsonPropertyName("authors")] public List<Author>? Authors { get; set; }
        [JsonPropertyName("logo")] public Asset? Logo { get; set; }
        [JsonPropertyName("links")] public Links? Links { get; set; }
        [JsonPropertyName("dateModified")] public string? DateModified { get; set; }
    }

    private sealed class Category
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("isClass")] public bool? IsClass { get; set; }
    }

    private sealed class Author
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    private sealed class Asset
    {
        [JsonPropertyName("thumbnailUrl")] public string? ThumbnailUrl { get; set; }
    }

    private sealed class Links
    {
        [JsonPropertyName("websiteUrl")] public string? WebsiteUrl { get; set; }
    }

    private sealed class FilesResponse
    {
        [JsonPropertyName("data")] public List<CfFile>? Data { get; set; }
    }

    private sealed class CfFile
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
        [JsonPropertyName("fileName")] public string? FileName { get; set; }
        [JsonPropertyName("releaseType")] public int ReleaseType { get; set; }
        [JsonPropertyName("downloadUrl")] public string? DownloadUrl { get; set; }
        [JsonPropertyName("fileDate")] public string? FileDate { get; set; }
        [JsonPropertyName("fileLength")] public long FileLength { get; set; }
        [JsonPropertyName("gameVersions")] public List<string>? GameVersions { get; set; }
    }
}
