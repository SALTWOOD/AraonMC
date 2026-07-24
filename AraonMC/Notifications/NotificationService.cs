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

using System.Threading;
using System.Threading.Tasks;
using AraonMC.Core.Application.Notifications;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace AraonMC.Notifications;

/// <summary>
/// UI-layer adapter that implements <see cref="INotificationService"/>. Owns the blocking
/// queue and spawns independent windows for non-blocking notifications. Resolves the
/// owner/main window dynamically from the running desktop lifetime, so it can be constructed
/// at any time.
/// </summary>
public sealed class NotificationService : INotificationService
{
    // Serializes blocking notifications so only one modal is on screen at a time.
    // Waiters resume on the UI thread (Avalonia installs a DispatcherSynchronizationContext),
    // which is required for ShowDialog.
    private readonly SemaphoreSlim _blockingGate = new(1, 1);

    // Cascade offset counter so multiple independent toasts don't stack exactly on top of each other.
    private int _cascadeIndex;

    private static Window? MainWindow =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

    public Task ShowAsync(NotificationRequest request, CancellationToken ct = default)
    {
        DebugLog.Info($"Notify: '{request.Title}' (level={request.Level}, mode={request.Mode}"
            + (request.AutoDismiss is { } d ? $", auto-dismiss={d.TotalSeconds:F1}s" : "") + ").");
        // 窗口创建/显示必须在 UI 线程；调用方可能在后台线程（launcher、下载后台任务），统一切回 UI 线程。
        if (Dispatcher.UIThread.CheckAccess())
            return ShowCoreAsync(request, ct);

        DebugLog.Info("Notify: caller is off the UI thread; marshalling ShowCore onto the UI thread.");
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.UIThread.Post(async () =>
        {
            try { await ShowCoreAsync(request, ct).ConfigureAwait(true); }
            catch (Exception ex) { DebugLog.Error($"Notify: UI-thread show threw ({ex.GetType().Name}: {ex.Message})."); tcs.SetException(ex); return; }
            tcs.SetResult();
        });
        return tcs.Task;
    }

    private async Task ShowCoreAsync(NotificationRequest request, CancellationToken ct)
    {
        if (request.Mode == NotificationMode.NonBlocking)
        {
            DebugLog.Info($"Notify: '{request.Title}' is non-blocking; spawning an independent toast.");
            SpawnIndependent(request);
            return;
        }

        DebugLog.Info($"Notify: '{request.Title}' is blocking; waiting for the modal gate...");
        await _blockingGate.WaitAsync(ct).ConfigureAwait(true);
        try
        {
            DebugLog.Info($"Notify: modal gate acquired for '{request.Title}'; showing dialog.");
            await ShowBlockingAsync(request);
        }
        finally
        {
            _blockingGate.Release();
            DebugLog.Info($"Notify: modal gate released after '{request.Title}'.");
        }
    }

    private static async Task ShowBlockingAsync(NotificationRequest request)
    {
        var window = Create(request);
        window.WindowStartupLocation = MainWindow is not null
            ? WindowStartupLocation.CenterOwner
            : WindowStartupLocation.CenterScreen;

        if (MainWindow is { } owner)
        {
            DebugLog.Info($"Notify: showing '{request.Title}' as modal dialog owned by main window.");
            await window.ShowDialog(owner);
        }
        else
        {
            DebugLog.Warn($"Notify: no main window yet; '{request.Title}' degraded to a non-modal show.");
            window.Show(); // No owner yet (startup only): degrade to a non-blocking show.
        }
    }

    private void SpawnIndependent(NotificationRequest request)
    {
        var window = Create(request);
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        PositionCascade(window);
        window.Show();
        DebugLog.Info($"Notify: toast '{request.Title}' shown at {window.Position}.");

        if (request.AutoDismiss is { } timeout && timeout > TimeSpan.Zero)
        {
            DebugLog.Info($"Notify: '{request.Title}' will auto-dismiss in {timeout.TotalSeconds:F1}s.");
            ScheduleAutoDismiss(window, timeout);
        }
    }

    private static void ScheduleAutoDismiss(Window window, TimeSpan timeout)
    {
        var timer = new DispatcherTimer { Interval = timeout };
        timer.Tick += OnTick;
        window.Closed += OnClosed;
        timer.Start();

        void OnTick(object? sender, EventArgs e)
        {
            timer.Stop();
            window.Close();
        }

        void OnClosed(object? sender, EventArgs e)
        {
            // Ensure the timer doesn't fire after the user closed the window manually.
            timer.Stop();
            window.Closed -= OnClosed;
        }
    }

    private void PositionCascade(Window window)
    {
        const double approxWidth = 380;
        const double approxHeight = 180;
        const double margin = 18;
        const double step = 34;
        const int wrap = 5;

        var screen = MainWindow?.Screens?.Primary;
        double scale = screen?.Scaling ?? 1d;
        double workWidth = (screen?.WorkingArea.Width ?? 1280) / scale;
        double workHeight = (screen?.WorkingArea.Height ?? 720) / scale;

        int index = Interlocked.Increment(ref _cascadeIndex) - 1;
        int slot = index % wrap;

        double x = workWidth - approxWidth - margin - slot * step;
        double y = workHeight - approxHeight - margin - slot * step;
        if (x < margin) x = margin;
        if (y < margin) y = margin;

        window.Position = new PixelPoint((int)Math.Round(x * scale), (int)Math.Round(y * scale));
    }

    private static NotificationWindow Create(NotificationRequest request) =>
        new() { DataContext = new NotificationWindowViewModel(request) };
}
