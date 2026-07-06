using Avalonia;
using Avalonia.Media;

namespace AraonMC.UI.Theme;

public class ColorResource
{
    public required byte A { get; init; }
    public required byte R { get; init; }
    public required byte G { get; init; }
    public required byte B { get; init; }
    public required string Suffix { get; init; }
    public bool ApplyToBrush { get; init; } = true;
    public bool ApplyToColor { get; init; } = true;

    public Color ToColor() => Color.FromArgb(A, R, G, B);

    public SolidColorBrush ToBrush() => new(ToColor());

    public void Apply()
    {
        var res = Application.Current!.Resources;
        var color = ToColor();
        if (ApplyToColor) res[$"ColorObject{Suffix}"] = color;
        if (ApplyToBrush) res[$"ColorBrush{Suffix}"] = new SolidColorBrush(color);
    }

    public void Apply(string colorKey, string brushKey)
    {
        var res = Application.Current!.Resources;
        var color = ToColor();
        res[colorKey] = color;
        res[brushKey] = new SolidColorBrush(color);
    }
}
