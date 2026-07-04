using System.IO.Compression;
using AraonMC.Core.Domain.Enums;
using AraonMC.LaunchArgs.Libraries;
using AraonMC.LaunchArgs.Rules;
using AraonMC.LaunchArgs.Version;
using AraonMC.Versions.Mirror;

namespace AraonMC.Versions.Install;

/// <summary>
/// 完整安装一个 Minecraft 版本：清单解析 → client.json → client.jar → libraries →
/// assets → logging → 解压 natives。所有 URL 按指定镜像改写。
/// </summary>
public sealed class VersionInstaller
{
    private readonly IVersionList _versions;
    private readonly DownloadMirror _mirror;
    private readonly HttpClient _http;
    private readonly FileDownloader _downloader;

    public VersionInstaller(IVersionList versions, DownloadMirror mirror, HttpClient http)
    {
        _versions = versions;
        _mirror = mirror;
        _http = http;
        _downloader = new FileDownloader(http);
    }

    public async Task InstallAsync(
        string versionId,
        string instanceDir,
        IProgress<InstallProgress>? progress = null,
        CancellationToken ct = default)
    {
        var entry = await _versions.ResolveAsync(versionId, ct).ConfigureAwait(false)
                    ?? throw new InstallException($"version '{versionId}' not found in manifest");

        Directory.CreateDirectory(instanceDir);
        var versionDir = Path.Combine(instanceDir, "versions", versionId);
        Directory.CreateDirectory(versionDir);

        // client.json（小文件，直下并解析）
        var clientJsonPath = Path.Combine(versionDir, versionId + ".json");
        await DownloadTextAsync(MirrorUrls.Rewrite(_mirror, entry.Url), clientJsonPath, ct);
        progress?.Report(new InstallProgress(InstallPhase.ClientJson, 1, 1, versionId + ".json"));

        var meta = VersionMetadataReader.Read(await File.ReadAllTextAsync(clientJsonPath, ct));
        var librariesRoot = Path.Combine(instanceDir, "libraries");

        // client.jar + libraries + logging 一起下
        var libReqs = new List<DownloadRequest>();
        if (meta.Downloads?.Client is { } client)
            libReqs.Add(MakeReq(client, Path.Combine(versionDir, versionId + ".jar")));

        var resolved = ResolveLibraries(meta);
        foreach (var r in resolved)
        {
            if (r.Library.Artifact is { } art)
                libReqs.Add(MakeReq(art, Path.Combine(librariesRoot, art.Path)));
            if (r.Native is { } native)
                libReqs.Add(MakeReq(native, Path.Combine(librariesRoot, native.Path)));
        }

        if (meta.Logging?.Client is { } log && !string.IsNullOrEmpty(log.Url) && !string.IsNullOrEmpty(log.Id))
        {
            libReqs.Add(MakeReq(
                new VersionArtifact { Url = log.Url, Sha1 = log.Sha1, Size = log.Size, Path = log.Id },
                Path.Combine(instanceDir, "assets", "log_configs", log.Id)));
        }

        await _downloader.DownloadAllAsync(libReqs, InstallPhase.Libraries, progress, ct);

        // assets
        if (meta.AssetIndex is { } ai)
        {
            var indexJsonPath = Path.Combine(instanceDir, "assets", "indexes", ai.Id + ".json");
            await DownloadTextAsync(MirrorUrls.Rewrite(_mirror, ToUri(ai.Url)), indexJsonPath, ct);
            var index = AssetIndex.Parse(await File.ReadAllTextAsync(indexJsonPath, ct));

            var objectsDir = Path.Combine(instanceDir, "assets", "objects");
            var assetReqs = new List<DownloadRequest>();
            foreach (var obj in index.Objects.Values)
            {
                if (string.IsNullOrEmpty(obj.Hash) || obj.Hash.Length < 2) continue;
                assetReqs.Add(new DownloadRequest
                {
                    Url = MirrorUrls.AssetUrl(_mirror, obj.Hash),
                    TargetPath = Path.Combine(objectsDir, obj.Hash[..2], obj.Hash),
                    Sha1 = obj.Hash,
                });
            }
            await _downloader.DownloadAllAsync(assetReqs, InstallPhase.Assets, progress, ct);
        }

        // 解压 natives
        progress?.Report(new InstallProgress(InstallPhase.Natives, 0, 0, null));
        ExtractNatives(resolved, librariesRoot, Path.Combine(instanceDir, "natives-" + versionId));
    }

    private static IReadOnlyList<ResolvedLibrary> ResolveLibraries(VersionMetadata meta)
    {
        // 复用 LaunchArgs 的选库逻辑，保证"下哪些"与"启动用哪些"一致。
        var resolver = new ClasspathResolver(new RuleEvaluator());
        return resolver.Resolve(meta.Libraries, new HashSet<string>());
    }

    private DownloadRequest MakeReq(VersionArtifact art, string targetPath) => new()
    {
        Url = MirrorUrls.Rewrite(_mirror, ToUri(art.Url)),
        TargetPath = targetPath,
        Sha1 = string.IsNullOrEmpty(art.Sha1) ? null : art.Sha1,
    };

    private async Task DownloadTextAsync(Uri url, string path, CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        using var resp = await _http.GetAsync(url, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        await File.WriteAllTextAsync(path, await resp.Content.ReadAsStringAsync(ct), ct).ConfigureAwait(false);
    }

    private static Uri ToUri(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var u)
            ? u
            : throw new InstallException($"invalid url: {url}");

    private static void ExtractNatives(IReadOnlyList<ResolvedLibrary> resolved, string librariesRoot, string nativesDir)
    {
        if (Directory.Exists(nativesDir)) Directory.Delete(nativesDir, recursive: true);
        Directory.CreateDirectory(nativesDir);

        foreach (var r in resolved)
        {
            if (r.Native is not { } native) continue;
            var nativePath = Path.Combine(librariesRoot, native.Path);
            if (!File.Exists(nativePath)) continue;

            var excludes = r.Library.Extract?.Exclude ?? [];
            using var archive = ZipFile.OpenRead(nativePath);
            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.EndsWith('/')) continue;
                if (excludes.Any(e => entry.FullName.StartsWith(e, StringComparison.OrdinalIgnoreCase))) continue;

                var dest = Path.Combine(nativesDir, entry.FullName);
                var entryDir = Path.GetDirectoryName(dest);
                if (!string.IsNullOrEmpty(entryDir)) Directory.CreateDirectory(entryDir);
                entry.ExtractToFile(dest, overwrite: true);
            }
        }
    }
}
