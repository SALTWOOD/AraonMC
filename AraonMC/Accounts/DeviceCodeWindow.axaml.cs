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
