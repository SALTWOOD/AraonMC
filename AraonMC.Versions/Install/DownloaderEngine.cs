using Downloader;

namespace AraonMC.Versions.Install;

/// <summary>基于 Downloader 库的传输引擎：大文件分块并行，小文件单块。</summary>
public sealed class DownloaderEngine : IDownloadEngine
{
    public async Task DownloadAsync(Uri url, string targetPath, IProgress<EngineProgress>? progress, CancellationToken ct)
    {
        var config = new DownloadConfiguration
        {
            ChunkCount = 8,
            ParallelDownload = true,
            MaxTryAgainOnFailover = 3,
            MinimumSizeOfChunking = 512 * 1024, // <0.5MB 不分块
        };

        var ds = new DownloadService(config);
        Exception? completionError = null;
        ds.DownloadProgressChanged += (_, e) =>
        {
            var received = (long)(e.ProgressPercentage / 100.0 * e.TotalBytesToReceive);
            progress?.Report(new EngineProgress(received, e.TotalBytesToReceive, e.BytesPerSecondSpeed));
        };
        // Downloader 默认“non-stopping”地吞掉传输异常，必须从完成事件里取真实错误。
        ds.DownloadFileCompleted += (_, e) => completionError = e.Error;

        using var reg = ct.Register(() => ds.CancelAsync());
        try
        {
            await ds.DownloadFileTaskAsync(url.OriginalString, targetPath).ConfigureAwait(false);
            await ds.CancelTaskAsync();
            await ds.DisposeAsync();
        }
        catch (Exception ex) when (ct.IsCancellationRequested)
        {
            throw new OperationCanceledException(ex.Message, ex, ct);
        }

        // 不主动检查，下游会对着不存在的 .tmp 做 sha1 校验而崩溃。
        if (completionError is not null)
            throw new InstallException($"download failed: {url}", url, targetPath, completionError);
        if (!File.Exists(targetPath))
            throw new InstallException($"download produced no file: {url}", url, targetPath);
    }
}
