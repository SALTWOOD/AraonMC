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
