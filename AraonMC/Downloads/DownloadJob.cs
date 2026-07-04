using MinecraftDownloader.Core.Models;
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

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsCancellable))] private DownloadStatus _status = DownloadStatus.Queued;
    [ObservableProperty] private double _progressPercent;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(BytesText))] private int _filesDone;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(BytesText))] private int _filesTotal;
    [ObservableProperty] private string? _errorMessage;

    /// <summary>是否仍可取消（运行/排队中）。</summary>
    public bool IsCancellable => Status is DownloadStatus.Running or DownloadStatus.Queued;

    /// <summary>上游只提供文件计数进度，故以“已完成/总文件数”展示。</summary>
    public string BytesText => FilesTotal > 0 ? $"{FilesDone} / {FilesTotal} files" : "—";

    public DownloadJob(string title, string versionId, string instancePath)
    {
        Title = title;
        VersionId = versionId;
        InstancePath = instancePath;
    }

    /// <summary>从上游 <see cref="DownloadProgress"/>（文件计数）同步进度字段。</summary>
    public void Apply(DownloadProgress p)
    {
        FilesDone = p.Completed;
        FilesTotal = p.Total;
        ProgressPercent = p.Total > 0 ? p.Completed * 100.0 / p.Total : ProgressPercent;
    }
}
