using System.Threading.Tasks;
using AraonMC.Core.Application.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AraonMC.ViewModels.Pages;

public partial class SettingsViewModel : PageViewModelBase
{
    private readonly INotificationService _notifications;

    public SettingsViewModel(INotificationService notifications)
    {
        _notifications = notifications;
        Title = "Settings";
    }

    public string AppVersion => "AraonMC 0.1.0 (dev)";

    // General
    [ObservableProperty] private string _language = "English (US)";
    [ObservableProperty] private bool _keepLauncherOpen = true;
    [ObservableProperty] private bool _discordRpc = false;
    [ObservableProperty] private bool _checkUpdatesOnStart = true;

    // Java
    [ObservableProperty] private string _javaPath = @"C:\Program Files\Java\jdk-21\bin\javaw.exe";
    [ObservableProperty] private string _javaArguments = "-Xmx4G -XX:+UseG1GC";
    [ObservableProperty] private double _maxMemoryMb = 4096;
    [ObservableProperty] private double _minMemoryMb = 512;

    // Game
    [ObservableProperty] private string _gameDirectory = @"%APPDATA%\.araonmc";
    [ObservableProperty] private double _windowWidth = 1280;
    [ObservableProperty] private double _windowHeight = 720;
    [ObservableProperty] private bool _fullscreen = false;

    [RelayCommand]
    private void Save()
    {
        // Persistence backend not implemented — values are UI-only.
    }

    // ---- Notification system demo triggers ----

    /// <summary>Blocking modal: only one shows at a time; the await completes when dismissed.</summary>
    [RelayCommand]
    private async Task ShowBlockingAsync()
    {
        await _notifications.ShowAsync(NotificationRequest.Dialog(
            "Settings saved",
            "Your preferences have been applied to this session.",
            NotificationLevel.Success));
    }

    /// <summary>Non-blocking toast: opens its own independent window immediately and auto-closes.</summary>
    [RelayCommand]
    private void ShowToast()
    {
        _notifications.ShowAsync(NotificationRequest.Toast(
            "Update available",
            "AraonMC 0.2.0 is ready to download.",
            NotificationLevel.Info));
    }

    /// <summary>Fires three blocking notifications back-to-back; they queue and display one-by-one.</summary>
    [RelayCommand]
    private async Task ShowQueueAsync()
    {
        var first = _notifications.ShowAsync(NotificationRequest.Dialog(
            "Step 1 of 3", "First queued notification.", NotificationLevel.Info));
        var second = _notifications.ShowAsync(NotificationRequest.Dialog(
            "Step 2 of 3", "Second queued notification.", NotificationLevel.Warning));
        var third = _notifications.ShowAsync(NotificationRequest.Dialog(
            "Step 3 of 3", "Third queued notification.", NotificationLevel.Error));
        await Task.WhenAll(first, second, third);
    }
}
