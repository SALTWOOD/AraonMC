using System.Collections.ObjectModel;
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

    void Cancel(DownloadJob job);
    void ClearFinished();
}

/// <summary>管理所有下载/安装任务的生命周期与进度。</summary>
public sealed class DownloadManager : IDownloadManager
{
    private readonly MinecraftInstaller _installer;
    private readonly NativeLibraryExtractor _natives;
    private readonly INotificationService _notifications;

    public DownloadManager(
        MinecraftInstaller installer,
        NativeLibraryExtractor natives,
        INotificationService notifications)
    {
        _installer = installer;
        _natives = natives;
        _notifications = notifications;
    }

    public ObservableCollection<DownloadJob> Jobs { get; } = new();

    public Task<DownloadJob> EnqueueAsync(GameInstance instance)
    {
        // 同一游戏目录下同一版本已有进行中的任务则不重复入队（共享 .minecraft 下多实例可能同路径不同版本）。
        var existing = Jobs.FirstOrDefault(j =>
            j.InstancePath == instance.Path
            && j.VersionId == instance.MinecraftVersion
            && j.Status is DownloadStatus.Running or DownloadStatus.Queued);
        if (existing is not null) return Task.FromResult(existing);

        var job = new DownloadJob(instance.Name, instance.MinecraftVersion, instance.Path);
        Jobs.Insert(0, job);
        _ = RunAsync(job);
        return Task.FromResult(job);
    }

    public void Cancel(DownloadJob job) => job.Cts.Cancel();

    public void ClearFinished()
    {
        for (var i = Jobs.Count - 1; i >= 0; i--)
            if (Jobs[i].Status is DownloadStatus.Completed or DownloadStatus.Failed or DownloadStatus.Cancelled)
                Jobs.RemoveAt(i);
    }

    private async Task RunAsync(DownloadJob job)
    {
        job.Status = DownloadStatus.Running;
        var progress = new Progress<DownloadProgress>(job.Apply);

        var request = new InstallRequest(
            GameVersion: job.VersionId,
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

            // 上游不下 native 库，安装期补下到 libraries/；解压留到启动期（见 MinecraftGameLauncher）。
            await _natives.EnsureDownloadedAsync(job.InstancePath, job.VersionId, job.Cts.Token);

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

    private async Task FailAsync(DownloadJob job, string message)
    {
        job.Status = DownloadStatus.Failed;
        job.ErrorMessage = message;
        await _notifications.ShowAsync(NotificationRequest.Toast(
            "Download failed", $"{job.Title}: {message}", NotificationLevel.Error));
    }
}
