using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AraonMC.Core.Config;
using AraonMC.UI.Theme;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AraonMC.ViewModels;

/// <summary>
/// One sidebar navigation entry. Appearance (tint / weight) derives from <see cref="IsActive"/>.
/// </summary>
public partial class NavItemViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _host;
    private readonly string _iconKey;

    public NavItemViewModel(MainWindowViewModel host, string iconKey, string label, PageViewModelBase page)
    {
        _host = host;
        _iconKey = iconKey;
        Label = label;
        Page = page;

        ThemeService.ColorModeChanged += OnThemeColorModeChanged;
        ThemeService.ColorThemeChanged += OnThemeColorThemeChanged;
    }

    private void OnThemeColorModeChanged(bool isDarkMode, ConfigEnums.ColorTheme theme)
    {
        OnPropertyChanged(nameof(ItemBackground));
        OnPropertyChanged(nameof(ItemForeground));
    }

    private void OnThemeColorThemeChanged(ConfigEnums.ColorTheme theme)
    {
        OnPropertyChanged(nameof(ItemBackground));
        OnPropertyChanged(nameof(ItemForeground));
    }

    public string Label { get; }
    public PageViewModelBase Page { get; }

    public Geometry Icon =>
        Application.Current!.TryFindResource(_iconKey, out var g) && g is Geometry geom ? geom : null!;

    [ObservableProperty] private bool _isActive;

    public IBrush ItemBackground =>
        IsActive ? Brush("ColorBrushSemiTransparent") : Brushes.Transparent;

    public IBrush ItemForeground =>
        IsActive ? Brush("ColorBrush4") : Brush("ColorBrushGray2");

    public FontWeight ItemWeight => IsActive ? FontWeight.SemiBold : FontWeight.Normal;

    [RelayCommand]
    private void Select() => _host.Navigate(this);

    partial void OnIsActiveChanged(bool value)
    {
        OnPropertyChanged(nameof(ItemBackground));
        OnPropertyChanged(nameof(ItemForeground));
        OnPropertyChanged(nameof(ItemWeight));
    }

    private static IBrush Brush(string key) =>
        Application.Current!.TryFindResource(key, out var b) && b is IBrush brush ? brush : Brushes.Transparent;
}
