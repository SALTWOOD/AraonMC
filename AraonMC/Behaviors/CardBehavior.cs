using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace AraonMC.Behaviors;

public static class CardBehavior
{
    public static readonly AttachedProperty<ICommand?> ReleaseCommandProperty =
        AvaloniaProperty.RegisterAttached<Border, ICommand?>("ReleaseCommand", typeof(CardBehavior));

    public static readonly AttachedProperty<object?> ReleaseCommandParameterProperty =
        AvaloniaProperty.RegisterAttached<Border, object?>("ReleaseCommandParameter", typeof(CardBehavior));

    public static ICommand? GetReleaseCommand(Border element) => element.GetValue(ReleaseCommandProperty);
    public static void SetReleaseCommand(Border element, ICommand? value) => element.SetValue(ReleaseCommandProperty, value);

    public static object? GetReleaseCommandParameter(Border element) => element.GetValue(ReleaseCommandParameterProperty);
    public static void SetReleaseCommandParameter(Border element, object? value) => element.SetValue(ReleaseCommandParameterProperty, value);

    static CardBehavior()
    {
        ReleaseCommandProperty.Changed.AddClassHandler<Border>((border, e) =>
        {
            if (e.NewValue is ICommand)
            {
                border.PointerPressed += OnPointerPressed;
                border.PointerReleased += OnPointerReleased;
            }
            else
            {
                border.PointerPressed -= OnPointerPressed;
                border.PointerReleased -= OnPointerReleased;
            }
        });
    }

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border) return;

        var point = e.GetCurrentPoint(border);
        if (!point.Properties.IsLeftButtonPressed) return;

        if (border.RenderTransform is not ScaleTransform st)
        {
            st = new ScaleTransform(1, 1);
            border.RenderTransform = st;
        }

        _ = ScaleTo(st, 0.97, 80);
    }

    private static void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not Border border) return;

        if (border.RenderTransform is ScaleTransform st)
        {
            _ = ScaleTo(st, 1.0, 160);
        }

        var command = border.GetValue(ReleaseCommandProperty);
        var parameter = border.GetValue(ReleaseCommandParameterProperty) ?? border.DataContext;
        if (command?.CanExecute(parameter) == true)
        {
            command.Execute(parameter);
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
            st.ScaleX = st.ScaleY = start + delta * eased;
            await Task.Delay(delay);
        }

        st.ScaleX = st.ScaleY = target;
    }
}
