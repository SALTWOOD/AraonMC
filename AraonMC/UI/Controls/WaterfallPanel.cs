using Avalonia;
using Avalonia.Controls;

namespace AraonMC.Controls;

public class WaterfallPanel : Panel
{
    public static readonly StyledProperty<double> ColumnWidthProperty =
        AvaloniaProperty.Register<WaterfallPanel, double>(nameof(ColumnWidth), 300);

    public static readonly StyledProperty<double> ColumnSpacingProperty =
        AvaloniaProperty.Register<WaterfallPanel, double>(nameof(ColumnSpacing), 18);

    public static readonly StyledProperty<double> RowSpacingProperty =
        AvaloniaProperty.Register<WaterfallPanel, double>(nameof(RowSpacing), 18);

    public double ColumnWidth
    {
        get => GetValue(ColumnWidthProperty);
        set => SetValue(ColumnWidthProperty, value);
    }

    public double ColumnSpacing
    {
        get => GetValue(ColumnSpacingProperty);
        set => SetValue(ColumnSpacingProperty, value);
    }

    public double RowSpacing
    {
        get => GetValue(RowSpacingProperty);
        set => SetValue(RowSpacingProperty, value);
    }

    private int _columnCount;
    private double[] _columnHeights = Array.Empty<double>();

    protected override Size MeasureOverride(Size availableSize)
    {
        var panelWidth = availableSize.Width;
        if (double.IsInfinity(panelWidth))
            panelWidth = 1000;

        var spacing = ColumnSpacing;
        _columnCount = Math.Max(1, (int)((panelWidth + spacing) / (ColumnWidth + spacing)));
        _columnHeights = new double[_columnCount];

        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            var colIndex = GetShortestColumn();
            child.Measure(new Size(ColumnWidth, double.PositiveInfinity));
            _columnHeights[colIndex] += child.DesiredSize.Height + RowSpacing;
        }

        var maxHeight = 0.0;
        for (int i = 0; i < _columnCount; i++)
        {
            if (_columnHeights[i] > maxHeight)
                maxHeight = _columnHeights[i];
        }

        maxHeight = Children.Count > 0 ? Math.Max(0, maxHeight - RowSpacing) : 0;
        return new Size(panelWidth, maxHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var spacing = ColumnSpacing;
        _columnCount = Math.Max(1, (int)((finalSize.Width + spacing) / (ColumnWidth + spacing)));
        _columnHeights = new double[_columnCount];

        var totalColumnsWidth = _columnCount * ColumnWidth + (_columnCount - 1) * spacing;
        var offsetX = Math.Max(0, (finalSize.Width - totalColumnsWidth) / 2);

        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            var colIndex = GetShortestColumn();

            var x = offsetX + colIndex * (ColumnWidth + spacing);
            var y = _columnHeights[colIndex];

            child.Arrange(new Rect(x, y, ColumnWidth, child.DesiredSize.Height));
            _columnHeights[colIndex] += child.DesiredSize.Height + RowSpacing;
        }

        return finalSize;
    }

    private int GetShortestColumn()
    {
        var minIndex = 0;
        var minHeight = _columnHeights[0];
        for (int i = 1; i < _columnHeights.Length; i++)
        {
            if (_columnHeights[i] < minHeight)
            {
                minHeight = _columnHeights[i];
                minIndex = i;
            }
        }
        return minIndex;
    }
}
