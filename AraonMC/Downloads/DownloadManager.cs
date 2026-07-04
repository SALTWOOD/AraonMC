using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AraonMC.Core.Application.Notifications;
using AraonMC.Core.Domain.Entities;
using MinecraftDownloader.Core.Models;
using MinecraftDownloader.Core.Orchestration;

namespace AraonMC.Downloads;

public interface IDownloadManager
{
    ObservableCollection<DownloadJob> Jobs { get; }

    /// <summary>把一个实例的版本安装任务入队（后台运行），返回任务句柄。</summary>
    Task<DownloadJob> EnqueueAsync(GameInstance instance);

    /// <summary>
    /// Enqueues a generic file download (e.g. a mod/resource) that streams <paramref name="url"/> to
    /// <paramref name="destPath"/>. Returns the job handle; the transfer runs in the background and reports
    /// byte progress. <paramref name="destPath"/> is taken verbatim (typically from a save-file dialog).
    /// </summary>
    Task<DownloadJob> EnqueueFileDownloadAsync(string title, string url, string destPath);

    void Cancel(DownloadJob job);
    void ClearFinished();
}

/// <summary>管理所有下载/安装任务的生命周期与进度（Minecraft 版本安装 + 通用文件下载）。</summary>
public sealed class DownloadManager : IDownloadManager
{
    private readonly MinecraftInstaller _installer;
    private readonly NativeLibraryExtractor _natives;
    private readonly HttpClient _http; // for generic file downloads.
    private readonly INotificationService _notifications;

    public DownloadManager(
        MinecraftInstaller installer,
        NativeLibraryExtractor natives,
        HttpClient http,
        INotificationService notifications)
    {
        _installer = installer;
        _natives = natives;
        _http = http;
        _notifications = notifications;
    }

    public ObservableCollection<DownloadJob> Jobs { get; } = new();

    public Task<DownloadJob> EnqueueAsync(GameInstance instance)
    {
        // 同一实例名（即 versions/<name>/ 目录）已有进行中的任务则不重复入队。
        var existing = Jobs.FirstOrDefault(j =>
            j.InstancePath == instance.Path
            && j.Detail == instance.MinecraftVersion
            && j.Status is DownloadStatus.Running or DownloadStatus.Queued);
        if (existing is not null) return Task.FromResult(existing);

        var job = new DownloadJob(instance.Name, instance.MinecraftVersion, instance.Path);
        Jobs.Insert(0, job);
        _ = RunAsync(job, instance);
        return Task.FromResult(job);
    }

    public Task<DownloadJob> EnqueueFileDownloadAsync(string title, string url, string destPath)
    {
        var fileName = Path.GetFileName(destPath);

        // Don't start a second concurrent transfer for the same destination.
        var existing = Jobs.FirstOrDefault(j =>
            j.DestinationPath == destPath && j.Status is DownloadStatus.Running or DownloadStatus.Queued);
        if (existing is not null) return Task.FromResult(existing);

        var job = new DownloadJob(title, fileName) { DestinationPath = destPath };
        Jobs.Insert(0, job);
        DebugLog.Info($"Downloads: enqueued file job '{title}' → {destPath} ({url}).");
        _ = RunFileAsync(job, url, destPath);
        return Task.FromResult(job);
    }

    public void Cancel(DownloadJob job) => job.Cts.Cancel();

    public void ClearFinished()
    {
        for (var i = Jobs.Count - 1; i >= 0; i--)
            if (Jobs[i].Status is DownloadStatus.Completed or DownloadStatus.Failed or DownloadStatus.Cancelled)
                Jobs.RemoveAt(i);
    }

    private async Task RunAsync(DownloadJob job, GameInstance instance)
    {
        job.Status = DownloadStatus.Running;
        var progress = new Progress<DownloadProgress>(job.Apply);

        var request = new InstallRequest(
            // Download Mojang's real base version, then rename versions/<base>/ to the instance name if needed.
            GameVersion: instance.BaseMinecraftVersion,
            GameDirectory: job.InstancePath,
            Loader: LoaderKind.Vanilla,
            MaxParallelism: 8,
            SkipIfExists: true);

        try
        {
            var result = await _installer.InstallAsync(request, progress, job.Cts.Token);

            if (!result.Succeeded)
            {
                if (result.Error is OperationCanceledException)
                {
                    job.Status = DownloadStatus.Cancelled;
                    return;
                }
                await FailAsync(job, result.Error?.Message ?? "installation failed");
                return;
            }

            // Bind the instance name to versions/<name>/ and <name>.jar/.json.
            await MoveInstalledVersionAsync(job.InstancePath, instance.BaseMinecraftVersion, instance.MinecraftVersion, job.Cts.Token);

            // 上游不下 native 库，安装期补下到 libraries/；解压留到启动期（见 MinecraftGameLauncher）。
            await _natives.EnsureDownloadedAsync(job.InstancePath, instance.MinecraftVersion, job.Cts.Token);

            job.Status = DownloadStatus.Completed;
            job.ProgressPercent = 100;
            await _notifications.ShowAsync(NotificationRequest.Toast(
                "Download complete", $"{job.Title} is ready to play.", NotificationLevel.Success));
        }
        catch (OperationCanceledException)
        {
            job.Status = DownloadStatus.Cancelled;
        }
        catch (Exception ex)
        {
            await FailAsync(job, ex.Message);
        }
    }

    private async Task RunFileAsync(DownloadJob job, string url, string destPath)
    {
        job.Status = DownloadStatus.Running;
        var dir = Path.GetDirectoryName(destPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        try
        {
            using var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, job.Cts.Token)
                .ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            var total = resp.Content.Headers.ContentLength ?? -1; // -1 = unknown length

            using var src = await resp.Content.ReadAsStreamAsync(job.Cts.Token).ConfigureAwait(false);
            using var dst = File.Create(destPath);
            var done = 0L;
            var buffer = new byte[81920];
            int read;
            while ((read = await src.ReadAsync(buffer.AsMemory(), job.Cts.Token).ConfigureAwait(false)) > 0)
            {
                await dst.WriteAsync(buffer.AsMemory(0, read), job.Cts.Token).ConfigureAwait(false);
                done += read;
                job.ReportBytes(done, total);
            }

            job.Status = DownloadStatus.Completed;
            job.ProgressPercent = 100;
            DebugLog.Info($"Downloads: file job '{job.Title}' complete → {destPath} ({done:N0} bytes).");
            await _notifications.ShowAsync(NotificationRequest.Toast(
                "Download complete", $"Saved '{job.Title}' to\n{Path.GetDirectoryName(destPath)}.", NotificationLevel.Success));
        }
        catch (OperationCanceledException)
        {
            job.Status = DownloadStatus.Cancelled;
            TryDelete(destPath);
        }
        catch (Exception ex)
        {
            DebugLog.Error($"Downloads: file job '{job.Title}' failed — {ex.Message}.");
            TryDelete(destPath);
            await FailAsync(job, ex.Message);
        }
    }

    private static Task MoveInstalledVersionAsync(string gameDir, string baseVersion, string instanceName, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (string.Equals(baseVersion, instanceName, StringComparison.Ordinal)) return Task.CompletedTask;

        var versionsRoot = Path.Combine(gameDir, "versions");
        var baseDir = Path.Combine(versionsRoot, baseVersion);
        var targetDir = Path.Combine(versionsRoot, instanceName);
        if (!Directory.Exists(baseDir))
            throw new DirectoryNotFoundException($"Expected installed version folder not found: {baseDir}");
        if (Directory.Exists(targetDir))
            throw new IOException($"Target version folder already exists: {targetDir}");

        RenameIfExists(Path.Combine(baseDir, baseVersion + ".jar"), Path.Combine(baseDir, instanceName + ".jar"));
        RenameIfExists(Path.Combine(baseDir, baseVersion + ".json"), Path.Combine(baseDir, instanceName + ".json"));
        Directory.Move(baseDir, targetDir);
        DebugLog.Info($"Downloads: renamed versions/{baseVersion} → versions/{instanceName}.");
        return Task.CompletedTask;
    }

    private async Task FailAsync(DownloadJob job, string message)
    {
        job.Status = DownloadStatus.Failed;
        job.ErrorMessage = message;
        await _notifications.ShowAsync(NotificationRequest.Toast(
            "Download failed", $"{job.Title}: {message}", NotificationLevel.Error));
    }

    /// <summary>Removes a partial file left behind by a cancelled/failed transfer (best effort).</summary>
    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* best effort */ }
    }

    private static void RenameIfExists(string oldPath, string newPath)
    {
        if (!File.Exists(oldPath)) return;
        if (File.Exists(newPath)) throw new IOException($"Target file already exists: {newPath}");
        File.Move(oldPath, newPath);
    }
}
