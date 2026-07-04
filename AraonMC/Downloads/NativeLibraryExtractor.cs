using System.IO.Compression;
using System.Security.Cryptography;
using AraonMC.LaunchArgs.Libraries;
using AraonMC.LaunchArgs.Rules;
using AraonMC.LaunchArgs.Version;

namespace AraonMC.Downloads;

/// <summary>
///   上游 <c>MinecraftDownloader.Core</c> 只下载每个库的 <c>artifact</c>，不下载也不解压
///   native 库（classifiers）。这里在 install 完成后补一步：按当前平台选出 native 制品，
///   下载到 libraries/&lt;path&gt;，再解压到 <c>natives-&lt;versionId&gt;</c>，供启动器
///   挂为 <c>-Djava.library.path</c>。选库逻辑复用 <see cref="ClasspathResolver"/>，
///   保证"装哪些 native"与"启动用哪些"一致。
/// </summary>
public sealed class NativeLibraryExtractor
{
    private readonly HttpClient _http;

    public NativeLibraryExtractor(HttpClient http) => _http = http;

    public async Task ExtractAsync(string gameDir, string versionId, CancellationToken ct = default)
    {
        var versionJsonPath = Path.Combine(gameDir, "versions", versionId, versionId + ".json");
        var json = await File.ReadAllTextAsync(versionJsonPath, ct).ConfigureAwait(false);
        var meta = VersionMetadataReader.Read(json);

        var resolved = new ClasspathResolver(new RuleEvaluator()).Resolve(meta.Libraries, new HashSet<string>());
        var librariesRoot = Path.Combine(gameDir, "libraries");
        var versionDir = Path.Combine(gameDir, "versions", versionId);
        // 真实 .minecraft 布局：natives 在版本目录内（versions/<id>/<id>-natives），
        // 也是启动时 -Djava.library.path 指向的位置。须与 MinecraftGameLauncher 一致。
        var nativesDir = Path.Combine(versionDir, versionId + "-natives");

        if (Directory.Exists(nativesDir)) Directory.Delete(nativesDir, recursive: true);
        Directory.CreateDirectory(nativesDir);

        foreach (var r in resolved)
        {
            if (r.Native is not { } native) continue;
            if (string.IsNullOrEmpty(native.Url) || string.IsNullOrEmpty(native.Path)) continue;

            var nativePath = Path.Combine(librariesRoot, native.Path);
            await DownloadIfMissingAsync(native.Url, nativePath, native.Sha1, ct).ConfigureAwait(false);
            ExtractNative(nativePath, nativesDir, r.Library.Extract?.Exclude);
        }
    }

    private async Task DownloadIfMissingAsync(string url, string dest, string expectedSha1, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(expectedSha1) && File.Exists(dest) && Sha1Matches(dest, expectedSha1))
            return;

        var dir = Path.GetDirectoryName(dest);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var tmp = dest + ".tmp";
        try
        {
            using var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();

            await using (var src = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false))
            await using (var dst = File.Create(tmp))
                await src.CopyToAsync(dst, ct).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(expectedSha1) && !Sha1Matches(tmp, expectedSha1))
                throw new InvalidDataException($"sha1 mismatch for native '{url}'.");

            File.Move(tmp, dest, overwrite: true);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }

    private static void ExtractNative(string jarPath, string nativesDir, IReadOnlyList<string>? excludes)
    {
        using var archive = ZipFile.OpenRead(jarPath);
        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.EndsWith('/')) continue;
            if (excludes is not null
                && excludes.Any(e => entry.FullName.StartsWith(e, StringComparison.OrdinalIgnoreCase)))
                continue;

            var dest = Path.Combine(nativesDir, entry.FullName);
            var entryDir = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(entryDir)) Directory.CreateDirectory(entryDir);
            entry.ExtractToFile(dest, overwrite: true);
        }
    }

    private static bool Sha1Matches(string path, string expected)
    {
        using var sha = SHA1.Create();
        using var stream = File.OpenRead(path);
        var hash = sha.ComputeHash(stream);
        return string.Equals(Convert.ToHexString(hash), expected, StringComparison.OrdinalIgnoreCase);
    }
}
