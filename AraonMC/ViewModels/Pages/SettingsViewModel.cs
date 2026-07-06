using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AraonMC.Core.Application.Notifications;
using AraonMC.Core.Config;
using AraonMC.UI.Theme;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreConfig = AraonMC.Core.Config.Config;

namespace AraonMC.ViewModels.Pages;

public partial class SettingsViewModel : PageViewModelBase
{
    private readonly INotificationService _notifications;
    private readonly Func<Task<string?>> _pickFolder;

    public SettingsViewModel(INotificationService notifications, Func<Task<string?>> pickFolder)
    {
        _notifications = notifications;
        _pickFolder = pickFolder;
        Title = "Settings";

        ColorModeOptions =
        [
            new Option<ConfigEnums.ColorMode>("Light", ConfigEnums.ColorMode.Light),
            new Option<ConfigEnums.ColorMode>("Dark", ConfigEnums.ColorMode.Dark),
            new Option<ConfigEnums.ColorMode>("System", ConfigEnums.ColorMode.System),
        ];
        ColorThemeOptions =
        [
            new Option<ConfigEnums.ColorTheme>("Sky Blue", ConfigEnums.ColorTheme.SkyBlue),
            new Option<ConfigEnums.ColorTheme>("Amber", ConfigEnums.ColorTheme.Amber),
        ];

        SelectedColorModeOption = ColorModeOptions.First(o => o.Value == CoreConfig.Theme.ColorMode);
        var currentTheme = ThemeService.CurrentTheme;
        SelectedColorThemeOption = ColorThemeOptions.First(o => o.Value == currentTheme);
    }

    public string AppVersion => "AraonMC 0.1.0 (dev)";

    // General
    [ObservableProperty] private string _language = "English (US)";
    [ObservableProperty] private bool _keepLauncherOpen = true;
    [ObservableProperty] private bool _discordRpc = false;
    [ObservableProperty] private bool _checkUpdatesOnStart = true;

    // Theme
    public IReadOnlyList<Option<ConfigEnums.ColorMode>> ColorModeOptions { get; }
    public IReadOnlyList<Option<ConfigEnums.ColorTheme>> ColorThemeOptions { get; }

    [ObservableProperty] private Option<ConfigEnums.ColorMode>? _selectedColorModeOption;
    [ObservableProperty] private Option<ConfigEnums.ColorTheme>? _selectedColorThemeOption;

    partial void OnSelectedColorModeOptionChanged(Option<ConfigEnums.ColorMode>? value)
    {
        if (value is null) return;
        DebugLog.Info($"Settings: color mode set to {value.Value}.");
        CoreConfig.Theme.ColorMode = value.Value;
        ThemeService.RefreshColorMode();
        
        if (SelectedColorThemeOption?.Value != ThemeService.CurrentTheme)
            SelectedColorThemeOption = ColorThemeOptions.First(o => o.Value == ThemeService.CurrentTheme);
    }

    partial void OnSelectedColorThemeOptionChanged(Option<ConfigEnums.ColorTheme>? value)
    {
        if (value is null) return;
        DebugLog.Info($"Settings: color theme set to {value.Value}.");
        ThemeService.CurrentTheme = value.Value;
        ThemeService.RefreshTheme();
    }

    // Java
    [ObservableProperty] private string _javaPath = @"C:\Program Files\Java\jdk-21\bin\javaw.exe";
    [ObservableProperty] private string _javaArguments = "-Xmx4G -XX:+UseG1GC";
    [ObservableProperty] private double _maxMemoryMb = 4096;
    [ObservableProperty] private double _minMemoryMb = 512;

    // Game
    /// <summary>读写穿透到 <c>Config.Game.GameDirectory</c>；空表示用系统标准 .minecraft 路径。</summary>
    public string GameDirectory
    {
        get => CoreConfig.Game.GameDirectory;
        set
        {
            CoreConfig.Game.GameDirectory = value;
            OnPropertyChanged();
        }
    }

    public string GameDirectoryDefault => ConfigPaths.DefaultGameDirectory();

    [RelayCommand]
    private async Task BrowseGameDirectoryAsync()
    {
        DebugLog.Info("Settings: opening folder picker for game directory...");
        var picked = await _pickFolder();
        if (picked is not null)
        {
            GameDirectory = picked;
            DebugLog.Info($"Settings: game directory set to '{picked}'.");
        }
        else
        {
            DebugLog.Info("Settings: game-directory folder picker cancelled.");
        }
    }

    [ObservableProperty] private double _windowWidth = 1280;
    [ObservableProperty] private double _windowHeight = 720;
    [ObservableProperty] private bool _fullscreen = false;

    [RelayCommand]
    private void Save()
    {
        DebugLog.Info("Settings: 'Save' pressed (values already bind live to the running config; persistence is a no-op for now).");
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
