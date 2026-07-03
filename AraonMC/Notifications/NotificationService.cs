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

    public async Task ShowAsync(NotificationRequest request, CancellationToken ct = default)
    {
        if (request.Mode == NotificationMode.NonBlocking)
        {
            SpawnIndependent(request);
            return;
        }

        await _blockingGate.WaitAsync(ct).ConfigureAwait(true);
        try
        {
            await ShowBlockingAsync(request);
        }
        finally
        {
            _blockingGate.Release();
        }
    }

    private static async Task ShowBlockingAsync(NotificationRequest request)
    {
        var window = Create(request);
        window.WindowStartupLocation = MainWindow is not null
            ? WindowStartupLocation.CenterOwner
            : WindowStartupLocation.CenterScreen;

        if (MainWindow is { } owner)
            await window.ShowDialog(owner);
        else
            window.Show(); // No owner yet (startup only): degrade to a non-blocking show.
    }

    private void SpawnIndependent(NotificationRequest request)
    {
        var window = Create(request);
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        PositionCascade(window);
        window.Show();

        if (request.AutoDismiss is { } timeout && timeout > TimeSpan.Zero)
        {
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
