using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using System.Windows.Input;

namespace AraonMC.Controls;

public class PaginationBar : TemplatedControl
{
    private Button? _prevButton;
    private Button? _nextButton;
    private TextBlock? _pageText;

    public static readonly StyledProperty<int> CurrentPageProperty =
        AvaloniaProperty.Register<PaginationBar, int>(nameof(CurrentPage), 1, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<int> TotalPagesProperty =
        AvaloniaProperty.Register<PaginationBar, int>(nameof(TotalPages), 1);

    public static readonly StyledProperty<ICommand?> PreviousPageCommandProperty =
        AvaloniaProperty.Register<PaginationBar, ICommand?>(nameof(PreviousPageCommand));

    public static readonly StyledProperty<ICommand?> NextPageCommandProperty =
        AvaloniaProperty.Register<PaginationBar, ICommand?>(nameof(NextPageCommand));

    public int CurrentPage
    {
        get => GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    public int TotalPages
    {
        get => GetValue(TotalPagesProperty);
        set => SetValue(TotalPagesProperty, value);
    }

    public ICommand? PreviousPageCommand
    {
        get => GetValue(PreviousPageCommandProperty);
        set => SetValue(PreviousPageCommandProperty, value);
    }

    public ICommand? NextPageCommand
    {
        get => GetValue(NextPageCommandProperty);
        set => SetValue(NextPageCommandProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_prevButton is not null)
            _prevButton.Click -= OnPrevClick;
        if (_nextButton is not null)
            _nextButton.Click -= OnNextClick;

        _prevButton = e.NameScope.Find<Button>("PART_PrevButton");
        _nextButton = e.NameScope.Find<Button>("PART_NextButton");
        _pageText = e.NameScope.Find<TextBlock>("PART_PageText");

        if (_prevButton is not null)
            _prevButton.Click += OnPrevClick;
        if (_nextButton is not null)
            _nextButton.Click += OnNextClick;

        UpdateState();
    }

    private void OnPrevClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (PreviousPageCommand?.CanExecute(null) == true)
            PreviousPageCommand.Execute(null);
    }

    private void OnNextClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (NextPageCommand?.CanExecute(null) == true)
            NextPageCommand.Execute(null);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CurrentPageProperty || change.Property == TotalPagesProperty)
            UpdateState();
    }

    private void UpdateState()
    {
        if (_prevButton is not null)
            _prevButton.IsEnabled = CurrentPage > 1;
        if (_nextButton is not null)
            _nextButton.IsEnabled = CurrentPage < TotalPages;
        if (_pageText is not null)
            _pageText.Text = $"{CurrentPage} / {TotalPages}";
    }
}
