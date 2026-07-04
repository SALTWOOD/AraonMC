using System.Collections.ObjectModel;
using AraonMC.Core.Application.Notifications;
using AraonMC.Core.Domain.Entities;
using AraonMC.Versions.Install;

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
    private readonly VersionInstaller _installer;
    private readonly INotificationService _notifications;

    public DownloadManager(VersionInstaller installer, INotificationService notifications)
    {
        _installer = installer;
        _notifications = notifications;
    }

    public ObservableCollection<DownloadJob> Jobs { get; } = new();

    public Task<DownloadJob> EnqueueAsync(GameInstance instance)
    {
        // 同一实例已有进行中的任务则不重复入队。
        var existing = Jobs.FirstOrDefault(j =>
            j.InstancePath == instance.Path && j.Status is DownloadStatus.Running or DownloadStatus.Queued);
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
        var progress = new Progress<InstallProgress>(job.Apply);
        try
        {
            await _installer.InstallAsync(job.VersionId, job.InstancePath, progress, job.Cts.Token);
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
            job.Status = DownloadStatus.Failed;
            job.ErrorMessage = ex.Message;
            await _notifications.ShowAsync(NotificationRequest.Toast(
                "Download failed", $"{job.Title}: {ex.Message}", NotificationLevel.Error));
        }
    }
}
