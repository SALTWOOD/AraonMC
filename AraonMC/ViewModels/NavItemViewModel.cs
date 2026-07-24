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
