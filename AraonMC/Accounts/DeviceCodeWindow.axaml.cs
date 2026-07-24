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

using System;
using System.Diagnostics;
using AraonMC.Auth;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;

namespace AraonMC.Accounts;

public partial class DeviceCodeWindow : Window
{
    /// <summary>Raised when the user clicks Cancel — the device-code UI cancels the login CTS.</summary>
    public event EventHandler? CancelRequested;

    private readonly DeviceCodeInfo _info;

    public DeviceCodeWindow()
    {
        InitializeComponent();
        _info = null!;
    }

    public DeviceCodeWindow(DeviceCodeInfo info)
    {
        InitializeComponent();
        _info = info;
        DataContext = new DeviceCodeWindowViewModel(info);
    }

    private async void Copy_Click(object? sender, RoutedEventArgs e)
    {
        var clipboard = GetTopLevel(this)?.Clipboard;
        if (clipboard is not null)
            await clipboard.SetTextAsync(_info.UserCode);
    }

    private void Open_Click(object? sender, RoutedEventArgs e)
    {
        var url = _info.DirectVerificationUrl ?? _info.VerificationUrl;
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { /* best-effort */ }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
        => CancelRequested?.Invoke(this, EventArgs.Empty);
}
