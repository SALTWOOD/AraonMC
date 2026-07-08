using System.Net.Http.Json;
using System.Text.Json;
using AraonMC.Core.Domain.Entities;

namespace AraonMC.Downloads;

public interface IVersionListService
{
    Task<IReadOnlyList<VersionPickerGroup>> GetGroupedVersionsAsync(CancellationToken ct = default);
}

/// <summary>Fetches the Minecraft version manifest with Mojang-primary / BMCLAPI-fallback and
/// groups versions like Modrinth's McVersionPicker (release → "X.Y", non-release → "X.Y Snapshots").</summary>
public sealed class VersionListService : IVersionListService
{
    private readonly HttpClient _http;

    private const string MojangManifest = "https://launchermeta.mojang.com/mc/game/version_manifest.json";
    private const string BmclapiManifest = "https://bmclapi2.bangbang93.com/mc/game/version_manifest.json";

    public VersionListService(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<VersionPickerGroup>> GetGroupedVersionsAsync(CancellationToken ct = default)
    {
        var versions = await FetchWithFallbackAsync(ct);
        versions.Sort((a, b) => b.ReleaseTime.CompareTo(a.ReleaseTime));
        return GroupVersions(versions);
    }

    private async Task<List<MinecraftVersion>> FetchWithFallbackAsync(CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(15));
            return await FetchManifestAsync(MojangManifest, cts.Token);
        }
        catch
        {
            DebugLog.Warn("Versions: Mojang manifest timed out; falling back to BMCLAPI.");
        }

        using var fallbackCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        fallbackCts.CancelAfter(TimeSpan.FromSeconds(30));
        return await FetchManifestAsync(BmclapiManifest, fallbackCts.Token);
    }

    private async Task<List<MinecraftVersion>> FetchManifestAsync(string url, CancellationToken ct)
    {
        var doc = await _http.GetFromJsonAsync<JsonDocument>(url, ct)
            ?? throw new InvalidOperationException("Empty manifest response.");

        var versions = new List<MinecraftVersion>();
        foreach (var entry in doc.RootElement.GetProperty("versions").EnumerateArray())
        {
            var id = entry.GetProperty("id").GetString() ?? string.Empty;
            var type = ParseVersionType(entry.GetProperty("type").GetString());
            var releaseTime = entry.GetProperty("releaseTime").GetDateTimeOffset();
            versions.Add(new MinecraftVersion { Id = id, Type = type, ReleaseTime = releaseTime });
        }

        return versions;
    }

    private static VersionType ParseVersionType(string? t) => t switch
    {
        "release" => VersionType.Release,
        "snapshot" => VersionType.Snapshot,
        "old_beta" => VersionType.OldBeta,
        "old_alpha" => VersionType.OldAlpha,
        _ => VersionType.Release,
    };

    private static IReadOnlyList<VersionPickerGroup> GroupVersions(List<MinecraftVersion> versions)
    {
        const string devReleaseKey = "Snapshots";
        var groups = new Dictionary<string, (List<MinecraftVersion> Versions, bool IsRelease)>();

        MinecraftVersion? currentRelease = null;

        foreach (var v in versions)
        {
            if (v.Type == VersionType.Release)
            {
                var key = GetMajorVersion(v.Id);
                if (!groups.TryGetValue(key, out var existing))
                    groups[key] = (new List<MinecraftVersion>(), true);
                groups[key].Versions.Add(v);
                currentRelease = v;
            }
            else
            {
                var major = currentRelease is not null
                    ? GetMajorVersion(currentRelease.Id)
                    : GetMajorVersion(v.Id);
                var key = $"{major} {devReleaseKey}";
                if (!groups.TryGetValue(key, out var existing))
                    groups[key] = (new List<MinecraftVersion>(), false);
                groups[key].Versions.Add(v);
            }
        }

        return groups
            .OrderByDescending(kv => MajorVersionSortKey(kv.Key))
            .Select(kv => new VersionPickerGroup(kv.Key, kv.Value.Versions, kv.Value.IsRelease))
            .ToList();
    }

    private static string GetMajorVersion(string id)
    {
        var parts = id.Split('.');
        return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : id;
    }

    private static (int major, int minor, bool isSnapshot) MajorVersionSortKey(string key)
    {
        var baseKey = key.Split(' ')[0];
        var parts = baseKey.Split('.');
        var major = parts.Length > 0 && int.TryParse(parts[0], out var m) ? m : 0;
        var minor = parts.Length > 1 && int.TryParse(parts[1], out var n) ? n : 0;
        return (major, minor, key.Contains("Snapshots"));
    }
}

public sealed record VersionPickerGroup(string Name, IReadOnlyList<MinecraftVersion> Versions, bool IsRelease);
