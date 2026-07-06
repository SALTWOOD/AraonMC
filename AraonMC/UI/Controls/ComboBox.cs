using AraonMC.Core.Config;
using AraonMC.UI.Theme;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System.Collections;
using System.Reflection;

namespace AraonMC.Controls;

public class ComboBox : TemplatedControl
{
    private Border? _border;
    private TextBlock? _selectedText;
    private Grid? _chevronContainer;
    private Popup? _popup;
    private Border? _dropdownBorder;
    private ListBox? _listBox;
    private RotateTransform? _rotate;
    private TopLevel? _topLevel;
    private DispatcherTimer? _animTimer;
    private DispatcherTimer? _fadeTimer;

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<ComboBox, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<ComboBox, object?>(nameof(SelectedItem),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<ComboBox, string?>(nameof(PlaceholderText), defaultValue: "Select...");

    public static readonly StyledProperty<bool> IsDropDownOpenProperty =
        AvaloniaProperty.Register<ComboBox, bool>(nameof(IsDropDownOpen));

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<ComboBox, IDataTemplate?>(nameof(ItemTemplate));

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public bool IsDropDownOpen
    {
        get => GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _animTimer?.Stop();
        _animTimer = null;
        _fadeTimer?.Stop();
        _fadeTimer = null;
        Unsubscribe();

        _border = e.NameScope.Find<Border>("PART_Border");
        _selectedText = e.NameScope.Find<TextBlock>("PART_SelectedText");
        _chevronContainer = e.NameScope.Find<Grid>("PART_ChevronContainer");
        _popup = e.NameScope.Find<Popup>("PART_Popup");
        _dropdownBorder = e.NameScope.Find<Border>("PART_DropdownBorder");
        _listBox = e.NameScope.Find<ListBox>("PART_ListBox");

        if (_chevronContainer != null)
        {
            _rotate = new RotateTransform(0);
            _chevronContainer.RenderTransform = _rotate;
        }

        Subscribe();

        UpdateSelectedText();
        UpdateChevronRotation(immediate: true);
        UpdateDropdownWidth();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        if (_topLevel != null)
        {
            _topLevel.AddHandler(PointerPressedEvent, OnRootPointerPressed, RoutingStrategies.Tunnel);
            if (_topLevel is Window w)
                w.Deactivated += OnWindowDeactivated;
        }
        ThemeService.ColorModeChanged += OnThemeChanged;
        ThemeService.ColorThemeChanged += OnThemeChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _animTimer?.Stop();
        _animTimer = null;
        _fadeTimer?.Stop();
        _fadeTimer = null;
        if (_topLevel != null)
        {
            _topLevel.RemoveHandler(PointerPressedEvent, OnRootPointerPressed);
            if (_topLevel is Window w)
                w.Deactivated -= OnWindowDeactivated;
            _topLevel = null;
        }
        ThemeService.ColorModeChanged -= OnThemeChanged;
        ThemeService.ColorThemeChanged -= OnThemeChanged;
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        IsDropDownOpen = false;
    }

    private void OnRootPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IsDropDownOpen)
            return;

        var source = e.Source as Visual;
        while (source != null)
        {
            if (source == _border || source == _dropdownBorder)
                return;
            source = source.Parent as Visual;
        }

        IsDropDownOpen = false;
    }

    private void Subscribe()
    {
        if (_border != null)
            _border.PointerPressed += OnBorderPointerPressed;
        if (_popup != null)
            _popup.Closed += OnPopupClosed;
        if (_listBox != null)
        {
            _listBox.SelectionChanged += OnListBoxSelectionChanged;
            _listBox.AddHandler(PointerPressedEvent, OnListBoxPointerPressed, RoutingStrategies.Tunnel);
        }
    }

    private void Unsubscribe()
    {
        _animTimer?.Stop();
        _animTimer = null;
        _fadeTimer?.Stop();
        _fadeTimer = null;
        if (_border != null)
            _border.PointerPressed -= OnBorderPointerPressed;
        if (_popup != null)
            _popup.Closed -= OnPopupClosed;
        if (_listBox != null)
        {
            _listBox.SelectionChanged -= OnListBoxSelectionChanged;
            _listBox.RemoveHandler(PointerPressedEvent, OnListBoxPointerPressed);
        }
    }

    private void OnBorderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        IsDropDownOpen = !IsDropDownOpen;
        e.Handled = true;
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        IsDropDownOpen = false;
    }

    private void OnListBoxSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_listBox?.SelectedItem != null)
        {
            SelectedItem = _listBox.SelectedItem;
            IsDropDownOpen = false;
        }
    }

    private void OnListBoxPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var source = e.Source as Visual;
        while (source != null)
        {
            if (source is ListBoxItem item && item.DataContext != null)
            {
                SelectedItem = item.DataContext;
                IsDropDownOpen = false;
                e.Handled = true;
                return;
            }
            source = source.Parent as Visual;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedItemProperty)
            UpdateSelectedText();
        else if (change.Property == IsDropDownOpenProperty)
        {
            UpdateChevronRotation(immediate: false);
            if (IsDropDownOpen)
            {
                if (_dropdownBorder != null)
                    _dropdownBorder.Opacity = 0;
                if (_popup != null)
                    _popup.IsOpen = true;
                StartFadeIn();
                Dispatcher.UIThread.Post(UpdateDropdownWidth);
            }
            else
            {
                StartFadeOut();
            }
        }
        else if (change.Property == ItemsSourceProperty)
            UpdateSelectedText();
    }

    private void OnThemeChanged(bool isDarkMode, ConfigEnums.ColorTheme theme) => UpdateSelectedText();
    private void OnThemeChanged(ConfigEnums.ColorTheme theme) => UpdateSelectedText();

    private void UpdateSelectedText()
    {
        if (_selectedText == null) return;

        var item = SelectedItem;
        if (item != null)
        {
            var text = GetItemDisplayText(item);
            _selectedText.Text = text ?? PlaceholderText;
            _selectedText.FontWeight = FontWeight.SemiBold;
            _selectedText.Opacity = 1;
            _selectedText.Foreground = Application.Current?.FindResource("ColorBrushGray1") as IBrush;
        }
        else
        {
            _selectedText.Text = PlaceholderText;
            _selectedText.FontWeight = FontWeight.Medium;
            _selectedText.Opacity = 0.7;
            _selectedText.Foreground = Application.Current?.FindResource("ColorBrushGray2") as IBrush;
        }
    }

    private void UpdateChevronRotation(bool immediate)
    {
        _animTimer?.Stop();

        if (_rotate == null)
            return;

        var target = IsDropDownOpen ? 180.0 : 0.0;
        var from = _rotate.Angle;

        if (Math.Abs(from - target) < 0.5)
            return;

        if (immediate)
        {
            _rotate.Angle = target;
            return;
        }

        const double durationMs = 75.0;
        const int stepMs = 8;
        var steps = (int)(durationMs / stepMs);
        var step = 0;

        _animTimer = new DispatcherTimer();
        _animTimer.Interval = TimeSpan.FromMilliseconds(stepMs);
        _animTimer.Tick += (_, _) =>
        {
            step++;
            _rotate!.Angle = from + (target - from) * (step / (double)steps);
            if (step >= steps)
            {
                _rotate!.Angle = target;
                _animTimer!.Stop();
                _animTimer = null;
            }
        };
        _animTimer.Start();
    }

    private void StartFadeIn()
    {
        _fadeTimer?.Stop();
        if (_dropdownBorder == null) return;

        const double durationMs = 150.0;
        const int stepMs = 16;
        var steps = (int)(durationMs / stepMs);
        var step = 0;

        _fadeTimer = new DispatcherTimer();
        _fadeTimer.Interval = TimeSpan.FromMilliseconds(stepMs);
        _fadeTimer.Tick += (_, _) =>
        {
            step++;
            _dropdownBorder!.Opacity = step / (double)steps;
            if (step >= steps)
            {
                _dropdownBorder!.Opacity = 1;
                _fadeTimer!.Stop();
                _fadeTimer = null;
            }
        };
        _fadeTimer.Start();
    }

    private void StartFadeOut()
    {
        _fadeTimer?.Stop();
        if (_dropdownBorder == null) return;

        var from = _dropdownBorder.Opacity;
        const double durationMs = 150.0;
        const int stepMs = 16;
        var steps = (int)(durationMs / stepMs);
        var step = 0;

        _fadeTimer = new DispatcherTimer();
        _fadeTimer.Interval = TimeSpan.FromMilliseconds(stepMs);
        _fadeTimer.Tick += (_, _) =>
        {
            step++;
            _dropdownBorder!.Opacity = from * (1 - step / (double)steps);
            if (step >= steps)
            {
                _dropdownBorder!.Opacity = 0;
                _fadeTimer!.Stop();
                _fadeTimer = null;
                if (_popup != null)
                    _popup.IsOpen = false;
            }
        };
        _fadeTimer.Start();
    }

    private void UpdateDropdownWidth()
    {
        if (_dropdownBorder == null) return;
        var w = Bounds.Width;
        if (w > 0)
        {
            _dropdownBorder.MinWidth = w;
            _dropdownBorder.Width = w;
        }
    }

    private static string? GetItemDisplayText(object? item)
    {
        if (item == null) return null;
        var type = item.GetType();
        var labelProp = type.GetRuntimeProperty("Label");
        if (labelProp != null && labelProp.CanRead)
            return labelProp.GetValue(item)?.ToString();
        return item.ToString();
    }
}
