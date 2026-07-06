namespace AraonMC.UI.Theme;

public static class ThemeManager
{
    public static bool IsDarkMode => ThemeService.IsDarkMode;

    public static void ThemeRefresh()
    {
        ThemeService.RefreshTheme();
    }
}
