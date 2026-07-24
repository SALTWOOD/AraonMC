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
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace AraonMC.Controls;

public partial class SearchBox : UserControl
{
    public static readonly StyledProperty<string?> SearchTextProperty =
        AvaloniaProperty.Register<SearchBox, string?>(nameof(SearchText), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<string?> WatermarkTextProperty =
        AvaloniaProperty.Register<SearchBox, string?>(nameof(WatermarkText), defaultValue: "Click to search");

    private static readonly Cursor IBeamCursor = new(StandardCursorType.Ibeam);

    private TopLevel? _topLevel;

    public string? SearchText
    {
        get => GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    public string? WatermarkText
    {
        get => GetValue(WatermarkTextProperty);
        set => SetValue(WatermarkTextProperty, value);
    }

    public SearchBox()
    {
        InitializeComponent();

        SearchBorder.AddHandler(InputElement.PointerEnteredEvent, OnSearchPointerEnter);
        SearchBorder.AddHandler(InputElement.PointerExitedEvent, OnSearchPointerLeave);
        SearchBorder.PointerPressed += OnSearchPointerPressed;
        SearchTextBox.GotFocus += OnSearchGotFocus;
        SearchTextBox.LostFocus += OnSearchLostFocus;
    }

    private static void OnSearchPointerEnter(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = Application.Current?.FindResource("ColorBrushGray6") as IBrush;
        }
    }

    private static void OnSearchPointerLeave(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = Application.Current?.FindResource("ColorBrushGray5") as IBrush;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        if (_topLevel != null)
            _topLevel.AddHandler(PointerPressedEvent, OnRootPointerPressed, RoutingStrategies.Tunnel);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_topLevel != null)
        {
            _topLevel.RemoveHandler(PointerPressedEvent, OnRootPointerPressed);
            _topLevel = null;
        }
    }

    private void OnSearchPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        SearchTextBox.Focus();
    }

    private void OnSearchGotFocus(object? sender, RoutedEventArgs e)
    {
        SearchBorder.Cursor = IBeamCursor;
    }

    private void OnSearchLostFocus(object? sender, RoutedEventArgs e)
    {
        SearchBorder.Cursor = Cursor.Default;
    }

    private void OnRootPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!SearchTextBox.IsFocused)
            return;

        var source = e.Source as Visual;
        while (source != null)
        {
            if (source == SearchBorder)
                return;
            source = source.Parent as Visual;
        }

        TopLevel.GetTopLevel(this)?.FocusManager.Focus(null);
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        BtnClear.IsVisible = !string.IsNullOrEmpty(SearchTextBox.Text);
    }

    private void OnClearClick(object? sender, RoutedEventArgs e)
    {
        SearchTextBox.Text = "";
        SearchTextBox.Focus();
    }
}
