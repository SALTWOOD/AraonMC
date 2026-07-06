namespace AraonMC.UI.Theme;

public record ToneProfileConfig
{
    public static readonly ToneProfile DefaultLight = new(
        L1: 0.28,  L2: 0.44,  L3: 0.56,  L4: 0.68,
        L5: 0.95,  L6: 0.90,  L7: 0.84,  L8: 0.78,
        LBackground: 0.93
    );

    public static readonly ToneProfile DefaultDark = new(
        L1: 0.85,  L2: 0.65,  L3: 0.48,  L4: 0.32,
        L5: 0.26,  L6: 0.22,  L7: 0.17,  L8: 0.15,
        LBackground: 0.18, LForeground: 1, LWhite: 0.275
    );

    public ToneProfile Light { get => field ?? DefaultLight; init; } = null!;

    public ToneProfile Dark { get => field ?? DefaultDark; init; } = null!;
}
