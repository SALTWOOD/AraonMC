using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using AraonMC.Core.Domain.Entities;
using AraonMC.ViewModels.Pages;

namespace AraonMC.Views.Pages;

public partial class BrowseView : UserControl
{
    private Border? _pressedBorder;
    private ResourceInfo? _pressedResource;

    public BrowseView()
    {
        InitializeComponent();
    }

    private void ResourceCard_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border) return;

        var point = e.GetCurrentPoint(border);
        if (!point.Properties.IsLeftButtonPressed) return;

        if (border.RenderTransform is not ScaleTransform st)
        {
            st = new ScaleTransform(1, 1);
            border.RenderTransform = st;
        }

        _pressedBorder = border;
        _pressedResource = border.DataContext as ResourceInfo;

        _ = ScaleTo(st, 0.97, 80);
    }

    private void ResourceCard_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var releasedBorder = _pressedBorder;
        var releasedResource = _pressedResource;
        _pressedBorder = null;
        _pressedResource = null;

        if (releasedBorder?.RenderTransform is ScaleTransform st)
        {
            _ = ScaleTo(st, 1.0, 160);
        }

        if (sender is Border border
            && border == releasedBorder
            && releasedResource is not null
            && DataContext is BrowseViewModel vm
            && vm.DetailCommand.CanExecute(releasedResource))
        {
            vm.DetailCommand.Execute(releasedResource);
        }
    }

    private static async Task ScaleTo(ScaleTransform st, double target, int durationMs)
    {
        double start = st.ScaleX;
        double delta = target - start;
        int frames = 8;
        int delay = Math.Max(1, durationMs / frames);

        for (int i = 1; i <= frames; i++)
        {
            double t = (double)i / frames;
            double eased = t < 0.5
                ? 2 * t * t
                : 1 - Math.Pow(-2 * t + 2, 2) / 2;
            double value = start + delta * eased;
            st.ScaleX = st.ScaleY = value;
            await Task.Delay(delay);
        }

        st.ScaleX = st.ScaleY = target;
    }
}
