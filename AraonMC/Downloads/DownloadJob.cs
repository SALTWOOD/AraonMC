// Copyright (C) 2026 SALTWOOD and contributors
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

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

/// <summary>
/// An observable download/install job shown on the Downloads page. Backs two kinds of work:
/// <list type="bullet">
/// <item>Minecraft version installs — file-count progress via <see cref="Apply"/> (<see cref="Detail"/> is the version id).</item>
/// <item>Generic file downloads (mods / resource packs / …) — byte progress via <see cref="ReportBytes"/>
/// (<see cref="Detail"/> is the destination filename).</item>
/// </list>
/// </summary>
public partial class DownloadJob : ObservableObject
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public string Title { get; }

    /// <summary>Subtitle line: a Minecraft version id (installs) or the destination filename (file downloads).</summary>
    public string Detail { get; }

    /// <summary>Instance path for Minecraft installs; empty for generic file downloads.</summary>
    public string InstancePath { get; }

    /// <summary>Absolute destination path for generic file downloads; null for Minecraft installs.</summary>
    public string? DestinationPath { get; init; }

    public CancellationTokenSource Cts { get; } = new();

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsCancellable))] private DownloadStatus _status = DownloadStatus.Queued;
    [ObservableProperty] private double _progressPercent;

    // File-count progress (Minecraft installs).
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ProgressText))] private int _filesDone;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ProgressText))] private int _filesTotal;

    // Byte progress (generic file downloads); BytesTotal &lt; 0 means "unknown length".
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ProgressText))] private long _bytesDone;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ProgressText))] private long _bytesTotal;

    [ObservableProperty] private string? _errorMessage;

    /// <summary>是否仍可取消（运行/排队中）。</summary>
    public bool IsCancellable => Status is DownloadStatus.Running or DownloadStatus.Queued;

    /// <summary>Human-readable progress: byte transfer for file downloads, file count for installs.</summary>
    public string ProgressText =>
        BytesTotal > 0 ? $"{Fmt(BytesDone)} / {Fmt(BytesTotal)}"
        : BytesTotal < 0 ? $"{Fmt(BytesDone)}"
        : FilesTotal > 0 ? $"{FilesDone} / {FilesTotal} files"
        : "—";

    public DownloadJob(string title, string detail, string instancePath = "")
    {
        Title = title;
        Detail = detail;
        InstancePath = instancePath ?? string.Empty;
    }

    /// <summary>从上游 <see cref="DownloadProgress"/>（文件计数）同步进度字段。</summary>
    public void Apply(DownloadProgress p)
    {
        FilesDone = p.Completed;
        FilesTotal = p.Total;
        ProgressPercent = p.Total > 0 ? p.Completed * 100.0 / p.Total : ProgressPercent;
    }

    /// <summary>Syncs byte progress for a generic file download. Pass <paramref name="total"/> &lt; 0 when unknown.</summary>
    public void ReportBytes(long done, long total)
    {
        BytesDone = done;
        BytesTotal = total;
        ProgressPercent = total > 0 ? done * 100.0 / total : ProgressPercent;
    }

    private static string Fmt(long bytes) => bytes switch
    {
        >= 1L << 20 => $"{bytes / (double)(1L << 20):0.#} MB",
        >= 1L << 10 => $"{bytes / (double)(1L << 10):0.#} KB",
        _ => $"{bytes} B",
    };
}
