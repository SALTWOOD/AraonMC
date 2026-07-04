using System.Text.Json.Nodes;
using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Enums;
using AraonMC.Versions.Mirror;

namespace AraonMC.Versions;

/// <summary>
/// 基于 HTTP 的版本清单源基类：抓取 manifest 并解析。Official / BMCLAPI 仅镜像不同。
/// manifest 在首次抓取后缓存于实例生命周期内。
/// </summary>
public abstract class HttpVersionList : IVersionList
{
    private readonly DownloadMirror _mirror;
    private readonly HttpClient _http;
    private VersionManifest? _manifest;

    protected HttpVersionList(DownloadMirror mirror, HttpClient http)
    {
        _mirror = mirror;
        _http = http;
    }

    public async Task<IReadOnlyList<MinecraftVersion>> GetVersionsAsync(CancellationToken ct = default)
    {
        var manifest = await GetManifestAsync(ct).ConfigureAwait(false);
        return manifest.Versions.Select(ToMinecraftVersion).ToList();
    }

    public async Task<VersionManifest.Entry?> ResolveAsync(string versionId, CancellationToken ct = default)
    {
        var manifest = await GetManifestAsync(ct).ConfigureAwait(false);
        return manifest.Versions.FirstOrDefault(v =>
            string.Equals(v.Id, versionId, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<VersionManifest> GetManifestAsync(CancellationToken ct)
    {
        if (_manifest is not null) return _manifest;

        using var resp = await _http.GetAsync(MirrorUrls.ManifestUrl(_mirror), ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _manifest = ParseManifest(body);
        return _manifest;
    }

    private static VersionManifest ParseManifest(string json)
    {
        var root = JsonNode.Parse(json)?.AsObject()
                   ?? throw new InvalidDataException("version manifest is empty");

        var latest = root["latest"]?.AsObject();
        var versions = root["versions"]?.AsArray()
                           ?.Where(n => n is not null)
                           .Select(n => ParseEntry(n!.AsObject()))
                           .ToList()
                       ?? new List<VersionManifest.Entry>();

        return new VersionManifest
        {
            LatestRelease = (string?)latest?["release"] ?? string.Empty,
            LatestSnapshot = (string?)latest?["snapshot"] ?? string.Empty,
            Versions = versions,
        };
    }

    private static VersionManifest.Entry ParseEntry(JsonObject o)
    {
        var urlStr = (string?)o["url"] ?? string.Empty;
        Uri.TryCreate(urlStr, UriKind.Absolute, out var url);
        DateTimeOffset.TryParse((string?)o["releaseTime"], out var rel);
        return new VersionManifest.Entry
        {
            Id = (string?)o["id"] ?? string.Empty,
            Type = (string?)o["type"] ?? string.Empty,
            Url = url ?? new Uri("https://localhost/"),
            ReleaseTime = rel,
        };
    }

    private static MinecraftVersion ToMinecraftVersion(VersionManifest.Entry e) => new()
    {
        Id = e.Id,
        Type = ParseType(e.Type),
        ReleaseTime = e.ReleaseTime,
    };

    private static VersionType ParseType(string? t) => t switch
    {
        "release" => VersionType.Release,
        "snapshot" => VersionType.Snapshot,
        "old_beta" => VersionType.OldBeta,
        "old_alpha" => VersionType.OldAlpha,
        _ => VersionType.Release,
    };
}
