using Avalonia.Media;
using Wacton.Unicolour;

namespace AraonMC.UI.Theme;

public class LabColor
{
    internal Unicolour UnicolourInstance { get; }

    private Oklab _Oklab { get => field ??= UnicolourInstance.Oklab; } = null!;

    public double L => _Oklab.L;
    public double A => _Oklab.A;
    public double B => _Oklab.B;
    public double Alpha { get; }

    public override int GetHashCode() => HashCode.Combine(L, A, B, Alpha);

    public override bool Equals(object? obj) => (obj is LabColor c) && c.L == L && c.A == A && c.B == B && c.Alpha == Alpha;

    private LabColor(double l, double a, double b, double alpha)
    {
        Alpha = alpha;
        UnicolourInstance = new Unicolour(ColourSpace.Oklab, l, a, b, alpha);
    }

    private LabColor(Unicolour instance)
    {
        Alpha = instance.Alpha.A;
        UnicolourInstance = instance;
    }

    internal static LabColor CreateInternal(Unicolour instance) => new(instance);

    public static LabColor Create(double l, double a, double b, double alpha = 1.0) => new(l, a, b, alpha);

    public static LabColor FromLch(double l, double c = 0, double h = 0, double alpha = 1.0)
    {
        var unicolour = new Unicolour(ColourSpace.Oklch, l, c, h, alpha);
        return CreateInternal(unicolour);
    }

    public static LabColor FromRgb(byte r, byte g, byte b, double alpha = 1.0)
    {
        var unicolour = new Unicolour(ColourSpace.Rgb255, r, g, b, alpha);
        return CreateInternal(unicolour);
    }

    public static LabColor FromRgb(double r, double g, double b, double alpha = 1.0)
    {
        var unicolour = new Unicolour(ColourSpace.RgbLinear, r, g, b, alpha);
        return CreateInternal(unicolour);
    }

    public static LabColor FromAvaloniaColor(Color color)
        => FromRgb(color.R, color.G, color.B, color.A / 255.0);

    public static implicit operator LabColor(Color color) => FromAvaloniaColor(color);

    public enum ScRgbMappingMode
    {
        Disable = -1,
        Clip = GamutMap.RgbClipping,
        ReduceChroma = GamutMap.OklchChromaReduction,
        ReducePurity = GamutMap.WxyPurityReduction,
    }

    public static ScRgbMappingMode ScRgbMapping { get; set; } = ScRgbMappingMode.ReduceChroma;

    public (byte A, byte R, byte G, byte B) ToSrgbBytes()
    {
        var instance = (ScRgbMapping == ScRgbMappingMode.Disable)
            ? UnicolourInstance
            : UnicolourInstance.MapToRgbGamut((GamutMap)ScRgbMapping);
        var r = (byte)instance.Rgb.Byte255.R;
        var g = (byte)instance.Rgb.Byte255.G;
        var b = (byte)instance.Rgb.Byte255.B;
        var a = (byte)(instance.Alpha.A * 255);
        return (a, r, g, b);
    }

    private bool _isSrgbInitialized;

    public (byte A, byte R, byte G, byte B) SrgbBytes
    {
        get
        {
            if (_isSrgbInitialized) return field;
            _isSrgbInitialized = true;
            return field = ToSrgbBytes();
        }
    } = default;

    public Color ToAvaloniaColor()
    {
        var (a, r, g, b) = SrgbBytes;
        return Color.FromArgb(a, r, g, b);
    }

    public static implicit operator Color(LabColor lab) => lab.ToAvaloniaColor();

    public ColorResource ToColorResource(string suffix, bool applyToColor = true, bool applyToBrush = true)
    {
        var (a, r, g, b) = SrgbBytes;
        return new ColorResource
        {
            A = a, R = r, G = g, B = b,
            ApplyToBrush = applyToBrush,
            ApplyToColor = applyToColor,
            Suffix = suffix
        };
    }

    internal static Unicolour MixInternal(LabColor c1, LabColor c2, double c1Amount = 0.5)
        => c1.UnicolourInstance.Mix(c2.UnicolourInstance, ColourSpace.Oklab, c1Amount);

    public static LabColor Mix(LabColor c1, LabColor c2, double c1Amount = 0.5) => CreateInternal(MixInternal(c1, c2, c1Amount));

    public static LabColor operator +(LabColor c1, LabColor c2) => Mix(c1, c2);
}
