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

    private static IBrush? _surfaceAltBrush;
    private static IBrush? _surfaceBrush;

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
            _surfaceAltBrush ??= Application.Current?.FindResource("SurfaceAltBrush") as IBrush;
            border.Background = _surfaceAltBrush;
        }
    }

    private static void OnSearchPointerLeave(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            _surfaceBrush ??= Application.Current?.FindResource("SurfaceBrush") as IBrush;
            border.Background = _surfaceBrush;
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
