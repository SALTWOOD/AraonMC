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

    private static readonly AttachedProperty<Helper?> HelperProperty =
        AvaloniaProperty.RegisterAttached<Border, Helper?>("CardBehaviorHelper", typeof(CardBehavior));

    public static ICommand? GetReleaseCommand(Border element) => element.GetValue(ReleaseCommandProperty);
    public static void SetReleaseCommand(Border element, ICommand? value) => element.SetValue(ReleaseCommandProperty, value);

    public static object? GetReleaseCommandParameter(Border element) => element.GetValue(ReleaseCommandParameterProperty);
    public static void SetReleaseCommandParameter(Border element, object? value) => element.SetValue(ReleaseCommandParameterProperty, value);

    static CardBehavior()
    {
        ReleaseCommandProperty.Changed.AddClassHandler<Border>(OnReleaseCommandChanged);
    }

    private static void OnReleaseCommandChanged(Border border, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is ICommand)
        {
            if (border.GetValue(HelperProperty) is null)
            {
                var helper = new Helper(border);
                border.SetValue(HelperProperty, helper);
                border.DetachedFromVisualTree += OnDetached;
            }
        }
        else
        {
            Detach(border);
        }
    }

    private static void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is Border border)
            Detach(border);
    }

    private static void Detach(Border border)
    {
        border.DetachedFromVisualTree -= OnDetached;
        var helper = border.GetValue(HelperProperty);
        if (helper is not null)
        {
            helper.Dispose();
            border.SetValue(HelperProperty, null);
        }
    }

    private sealed class Helper : IDisposable
    {
        private readonly Border _border;
        private CancellationTokenSource? _animCts;
        private bool _pressed;
        private bool _disposed;

        public Helper(Border border)
        {
            _border = border;
            _border.PointerPressed += OnPointerPressed;
            _border.PointerReleased += OnPointerReleased;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            CancelAnim();
            _border.PointerPressed -= OnPointerPressed;
            _border.PointerReleased -= OnPointerReleased;
        }

        private void CancelAnim()
        {
            _animCts?.Cancel();
            _animCts?.Dispose();
            _animCts = null;
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var point = e.GetCurrentPoint(_border);
            if (!point.Properties.IsLeftButtonPressed) return;

            _pressed = true;

            if (_border.RenderTransform is not ScaleTransform st)
            {
                st = new ScaleTransform(1, 1);
                _border.RenderTransform = st;
            }

            CancelAnim();
            _animCts = new CancellationTokenSource();
            _ = AnimateAsync(st, 0.97, 80, _animCts.Token);
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (!_pressed) return;
            _pressed = false;

            if (e.InitialPressMouseButton != MouseButton.Left) return;

            if (_border.RenderTransform is ScaleTransform st)
            {
                CancelAnim();
                _animCts = new CancellationTokenSource();
                _ = AnimateAsync(st, 1.0, 160, _animCts.Token);
            }

            var command = _border.GetValue(ReleaseCommandProperty);
            var parameter = _border.GetValue(ReleaseCommandParameterProperty) ?? _border.DataContext;
            if (command?.CanExecute(parameter) == true)
            {
                command.Execute(parameter);
            }
        }

        private static async Task AnimateAsync(ScaleTransform st, double target, int durationMs, CancellationToken ct)
        {
            try
            {
                double start = st.ScaleX;
                double delta = target - start;
                int frames = 8;
                int delay = Math.Max(1, durationMs / frames);

                for (int i = 1; i <= frames; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    double t = (double)i / frames;
                    double eased = t < 0.5
                        ? 2 * t * t
                        : 1 - Math.Pow(-2 * t + 2, 2) / 2;
                    st.ScaleX = st.ScaleY = start + delta * eased;
                    await Task.Delay(delay, ct);
                }

                st.ScaleX = st.ScaleY = target;
            }
            catch (OperationCanceledException) { }
        }
    }
}
