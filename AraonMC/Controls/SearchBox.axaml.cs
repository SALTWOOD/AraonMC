using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AraonMC.Controls;

public partial class SearchBox : UserControl
{
    public static readonly StyledProperty<string?> SearchTextProperty =
        AvaloniaProperty.Register<SearchBox, string?>(nameof(SearchText), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<string?> WatermarkTextProperty =
        AvaloniaProperty.Register<SearchBox, string?>(nameof(WatermarkText), defaultValue: "Click to search");

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

    private void OnSearchPointerEnter(object? sender, PointerEventArgs e)
    {
        SearchBorder.Background = this.FindResource("SurfaceAltBrush") as Avalonia.Media.IBrush;
    }

    private void OnSearchPointerLeave(object? sender, PointerEventArgs e)
    {
        SearchBorder.Background = this.FindResource("SurfaceBrush") as Avalonia.Media.IBrush;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
            topLevel.AddHandler(PointerPressedEvent, OnRootPointerPressed, RoutingStrategies.Tunnel);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
            topLevel.RemoveHandler(PointerPressedEvent, OnRootPointerPressed);
    }

    private void OnSearchPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        SearchTextBox.Focus();
    }

    private void OnSearchGotFocus(object? sender, RoutedEventArgs e)
    {
        SearchBorder.Cursor = new Cursor(StandardCursorType.Ibeam);
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

        TopLevel.GetTopLevel(this)?.FocusManager?.ClearFocus();
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
