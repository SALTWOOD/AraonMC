using System.Threading;
using System.Threading.Tasks;
using AraonMC.Auth;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace AraonMC.Accounts;

/// <summary>
/// <see cref="IDeviceCodeUI"/> for Avalonia: shows a modal <see cref="DeviceCodeWindow"/>.
/// Cancel aborts <see cref="CancelSource"/>, which the account service links to the login CTS.
/// </summary>
public sealed class AvaloniaDeviceCodeUI : IDeviceCodeUI
{
    /// <summary>Set by the account service per login; the dialog's Cancel button cancels it.</summary>
    public CancellationTokenSource? CancelSource { get; set; }

    public async Task DisplayAsync(DeviceCodeInfo info, CancellationToken cancellationToken)
    {
        var window = new DeviceCodeWindow(info);
        window.CancelRequested += (_, _) => CancelSource?.Cancel();

        // Authenticator cancels this token when polling ends → close on the UI thread.
        using var registration = cancellationToken.Register(
            () => Dispatcher.UIThread.Post(window.Close));

        var owner = ResolveMainWindow();
        if (owner is null)
        {
            // No main window yet (startup race) — degrade to a non-blocking show + wait.
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Show();
            try { await Task.Delay(Timeout.Infinite, cancellationToken); }
            catch (OperationCanceledException) { }
            window.Close();
            return;
        }

        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        await window.ShowDialog(owner);
    }

    private static Window? ResolveMainWindow() =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
}
