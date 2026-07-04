using System.Security.Cryptography;

namespace AraonMC.Versions.Install;

/// <summary>单个下载请求。</summary>
public sealed class DownloadRequest
{
    public required Uri Url { get; init; }
    public required string TargetPath { get; init; }

    /// <summary>期望的 sha1；提供时用于校验与"已存在则跳过"判定。</summary>
    public string? Sha1 { get; init; }

    /// <summary>文件大小（字节）；用于汇总总进度。未知则留 null。</summary>
    public long? Size { get; init; }
}

/// <summary>
/// 并发文件下载编排器：跨文件并行（默认 8）+ 每文件委托 <see cref="IDownloadEngine"/> 传输；
/// sha1 校验、原子写入（.tmp → move）、已存在且校验通过则跳过；聚合字节级进度。
/// </summary>
public sealed class FileDownloader
{
    private readonly IDownloadEngine _engine;
    private readonly int _maxConcurrency;

    public FileDownloader(IDownloadEngine engine, int maxConcurrency = 8)
    {
        _engine = engine;
        _maxConcurrency = maxConcurrency;
    }

    public async Task DownloadAllAsync(
        IReadOnlyList<DownloadRequest> requests,
        InstallPhase phase,
        IProgress<InstallProgress>? progress,
        CancellationToken ct = default)
    {
        var totalFiles = requests.Count;
        var totalBytes = requests.Sum(r => r.Size ?? 0);
        var received = new long[totalFiles];
        var speed = new long[totalFiles];
        var filesDone = 0;
        var lastReport = 0L;

        void Report(string? currentFile, bool force)
        {
            var now = Environment.TickCount64;
            if (!force && now - lastReport < 100) return;
            lastReport = now;

            var recv = 0L;
            for (var i = 0; i < received.Length; i++) recv += received[i];
            var sp = 0L;
            for (var i = 0; i < speed.Length; i++) sp += speed[i];

            progress?.Report(new InstallProgress(phase, recv, totalBytes, filesDone, totalFiles, sp, currentFile));
        }

        async Task DownloadOneAsync(DownloadRequest req, int idx, CancellationToken token)
        {
            var dir = Path.GetDirectoryName(req.TargetPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            if (req.Sha1 is not null && File.Exists(req.TargetPath) && Sha1Matches(req.TargetPath, req.Sha1))
            {
                received[idx] = req.Size ?? 0;
                return;
            }

            var tmp = req.TargetPath + ".tmp";
            IProgress<EngineProgress>? perFile = progress is null ? null
                : new Progress<EngineProgress>(p =>
                {
                    received[idx] = p.ReceivedBytes;
                    speed[idx] = (long)p.BytesPerSecond;
                    Report(Path.GetFileName(req.TargetPath), force: false);
                });

            // 镜像偶发抽风时单文件重试，避免整批因一个文件失败。
            Exception? lastError = null;
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                token.ThrowIfCancellationRequested();
                received[idx] = 0;
                if (File.Exists(tmp)) File.Delete(tmp);

                try
                {
                    await _engine.DownloadAsync(req.Url, tmp, perFile, token).ConfigureAwait(false);
                    if (req.Sha1 is not null && !Sha1Matches(tmp, req.Sha1))
                        throw new InstallException($"sha1 mismatch for {req.Url}", req.Url, req.TargetPath);
                    File.Move(tmp, req.TargetPath, overwrite: true);
                    if (req.Size is { } sz) received[idx] = sz;
                    return;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { lastError = ex; }
            }
            throw new InstallException($"download failed after 3 attempts: {req.Url}", req.Url, req.TargetPath, lastError);
        }

        var indexed = requests.Select((r, i) => (req: r, idx: i)).ToArray();
        await Parallel.ForEachAsync(
            indexed,
            new ParallelOptions { MaxDegreeOfParallelism = _maxConcurrency, CancellationToken = ct },
            async (item, token) =>
            {
                await DownloadOneAsync(item.req, item.idx, token).ConfigureAwait(false);
                Interlocked.Increment(ref filesDone);
                Report(Path.GetFileName(item.req.TargetPath), force: false);
            }).ConfigureAwait(false);

        Report(null, force: true);
    }

    private static bool Sha1Matches(string path, string expected)
    {
        using var sha = SHA1.Create();
        using var stream = File.OpenRead(path);
        var hash = sha.ComputeHash(stream);
        return string.Equals(Convert.ToHexString(hash), expected, StringComparison.OrdinalIgnoreCase);
    }
}
