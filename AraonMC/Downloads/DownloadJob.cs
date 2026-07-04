using System.Threading;
using AraonMC.Versions.Install;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AraonMC.Downloads;

public enum DownloadStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled,
}

/// <summary>一个可观测的下载任务（安装一个版本到某实例目录）。</summary>
public partial class DownloadJob : ObservableObject
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public string Title { get; }
    public string VersionId { get; }
    public string InstancePath { get; }
    public CancellationTokenSource Cts { get; } = new();

    [ObservableProperty] private DownloadStatus _status = DownloadStatus.Queued;
    [ObservableProperty] private double _progressPercent;
    [ObservableProperty] private long _receivedBytes;
    [ObservableProperty] private long _totalBytes;
    [ObservableProperty] private double _bytesPerSecond;
    [ObservableProperty] private string? _errorMessage;

    public DownloadJob(string title, string versionId, string instancePath)
    {
        Title = title;
        VersionId = versionId;
        InstancePath = instancePath;
    }

    /// <summary>从 <see cref="InstallProgress"/> 同步进度字段。</summary>
    public void Apply(InstallProgress p)
    {
        ReceivedBytes = p.ReceivedBytes;
        TotalBytes = p.TotalBytes;
        BytesPerSecond = p.BytesPerSecond;
        ProgressPercent = p.TotalBytes > 0 ? p.ReceivedBytes * 100.0 / p.TotalBytes : ProgressPercent;
    }
}
