using AraonMC.Core.Config;
using CoreConfig = AraonMC.Core.Config.Config;

namespace AraonMC.UI.Theme;

public delegate void ColorModeChangedEvent(bool isDarkMode, ConfigEnums.ColorTheme theme);
public delegate void ColorThemeChangedEvent(ConfigEnums.ColorTheme theme);

public static partial class ThemeService
{
    public static bool IsDarkMode { get; private set; }

    public static event ColorModeChangedEvent? ColorModeChanged;
    public static event ColorThemeChangedEvent? ColorThemeChanged;

    public static ToneProfileConfig ToneProfiles { get; set; } = new();

    public static void Initialize()
    {
        IsDarkMode = _IsDarkMode();
        _RefreshAll();
    }

    public static void RefreshColorMode()
    {
        var isDarkMode = _IsDarkMode();
        if (IsDarkMode == isDarkMode) return;
        IsDarkMode = isDarkMode;
        _RefreshAll();
    }

    private static bool _IsDarkMode() => CoreConfig.Theme.ColorMode switch
    {
        ConfigEnums.ColorMode.Light => false,
        ConfigEnums.ColorMode.Dark => true,
        ConfigEnums.ColorMode.System => OperatingSystem.IsWindows() && SystemThemeHelper.IsSystemInDarkMode(),
        _ => false
    };

    public static ToneProfile CurrentTone => IsDarkMode ? ToneProfiles.Dark : ToneProfiles.Light;

    public static ConfigEnums.ColorTheme CurrentTheme
    {
        get
        {
            var theme = CoreConfig.Theme;
            return IsDarkMode ? theme.DarkColor : theme.LightColor;
        }
        set
        {
            var theme = CoreConfig.Theme;
            if (IsDarkMode)
                theme.DarkColor = value;
            else
                theme.LightColor = value;
        }
    }

    public static (int Hue, double LightAdjust, double ChromaAdjust) GetCurrentThemeArgs()
    {
        var theme = CurrentTheme;
        return theme switch
        {
            ConfigEnums.ColorTheme.SkyBlue => (235, 0.36, 0.2),
            ConfigEnums.ColorTheme.Amber => (38, 0.15, 0.1),
            _ => (235, 0.36, 0.2)
        };
    }

    private static double _AdjustLinear(double value, double adjustment)
    {
        if (adjustment == 0) return value;
        value = Math.Clamp(value, 0.0, 1.0);
        adjustment = Math.Clamp(adjustment, -1.0, 1.0);
        return adjustment switch
        {
            > 0 => value + (1.0 - value) * adjustment,
            _ => value + value * adjustment
        };
    }

    private static ColorResource[] _CalculateGrays(ToneProfile tone) => [
        LabColor.FromLch(tone.L1).ToColorResource("Gray1"),
        LabColor.FromLch(tone.L2).ToColorResource("Gray2"),
        LabColor.FromLch(tone.L3).ToColorResource("Gray3"),
        LabColor.FromLch(tone.L4).ToColorResource("Gray4"),
        LabColor.FromLch(tone.L5).ToColorResource("Gray5"),
        LabColor.FromLch(tone.L6).ToColorResource("Gray6"),
        LabColor.FromLch(tone.L7).ToColorResource("Gray7"),
        LabColor.FromLch(tone.L8).ToColorResource("Gray8"),
        LabColor.FromLch(tone.LWhite, alpha: tone.AHalfWhite).ToColorResource("HalfWhite", false),
        LabColor.FromLch(tone.LWhite, alpha: tone.ASemiWhite).ToColorResource("SemiWhite", false),
        LabColor.FromLch(tone.LWhite).ToColorResource("White", false),
        LabColor.FromLch(tone.LWhite, alpha: tone.ATransparent).ToColorResource("Transparent", false),
        LabColor.FromLch(tone.LBackground, alpha: tone.ABackground).ToColorResource("TransparentBackground", false),
        LabColor.FromLch(tone.LBackground).ToColorResource("Background", false),
        LabColor.FromLch(tone.LBackground, alpha: tone.AToolTip).ToColorResource("ToolTip", false),
        LabColor.FromLch(tone.L7, 0.25, 30, tone.AHalfTransparent).ToColorResource("RedBack", false),
        LabColor.FromLch(tone.LForeground).ToColorResource("Memory", false),
    ];

    private static ColorResource[] _CalculateColors(ToneProfile tone, (int hue, double lightAdj, double chromaAdj) args) => [
        LabColor.FromLch(_AdjustLinear(tone.L1, args.lightAdj * 0.1), _AdjustLinear(tone.C1, args.chromaAdj * 0.25), args.hue).ToColorResource("1"),
        LabColor.FromLch(_AdjustLinear(tone.L2, args.lightAdj), _AdjustLinear(tone.C2, args.chromaAdj), args.hue).ToColorResource("2"),
        LabColor.FromLch(_AdjustLinear(tone.L3, args.lightAdj), _AdjustLinear(tone.C3, args.chromaAdj), args.hue).ToColorResource("3"),
        LabColor.FromLch(_AdjustLinear(tone.L4, args.lightAdj), _AdjustLinear(tone.C4, args.chromaAdj), args.hue).ToColorResource("4"),
        LabColor.FromLch(_AdjustLinear(tone.L5, args.lightAdj), _AdjustLinear(tone.C5, args.chromaAdj), args.hue).ToColorResource("5"),
        LabColor.FromLch(_AdjustLinear(tone.L6, args.lightAdj), _AdjustLinear(tone.C6, args.chromaAdj), args.hue).ToColorResource("6"),
        LabColor.FromLch(_AdjustLinear(tone.L7, args.lightAdj), _AdjustLinear(tone.C7, args.chromaAdj), args.hue).ToColorResource("7"),
        LabColor.FromLch(_AdjustLinear(tone.L8, args.lightAdj), _AdjustLinear(tone.C8, args.chromaAdj), args.hue).ToColorResource("8"),
        LabColor.FromLch(_AdjustLinear(tone.L8, args.lightAdj), _AdjustLinear(tone.C8, args.chromaAdj), args.hue, tone.ASemiTransparent).ToColorResource("SemiTransparent", false),
        LabColor.FromLch(_AdjustLinear(tone.L5, args.lightAdj), _AdjustLinear(tone.C5, args.chromaAdj), args.hue).ToColorResource("Bg0"),
        LabColor.FromLch(_AdjustLinear(tone.L7, args.lightAdj), _AdjustLinear(tone.C7, args.chromaAdj), args.hue, tone.ASemiWhite).ToColorResource("Bg1"),
    ];

    private static ColorResource[]? LightGrayCache;
    private static ColorResource[]? DarkGrayCache;

    public static void InvalidateGrayCache()
    {
        LightGrayCache = null;
        DarkGrayCache = null;
    }

    public static void ApplyGrayResources()
    {
        var cache = IsDarkMode
            ? (DarkGrayCache ??= _CalculateGrays(ToneProfiles.Dark))
            : (LightGrayCache ??= _CalculateGrays(ToneProfiles.Light));
        foreach (var c in cache) c.Apply();
    }

    public static void ApplyColorResources()
    {
        var colors = _CalculateColors(CurrentTone, GetCurrentThemeArgs());
        foreach (var c in colors) c.Apply();
    }

    private static void _RefreshAll()
    {
        ApplyGrayResources();
        ApplyColorResources();
        ColorModeChanged?.Invoke(IsDarkMode, CurrentTheme);
    }

    public static void RefreshTheme()
    {
        _RefreshAll();
        ColorThemeChanged?.Invoke(CurrentTheme);
    }


}
