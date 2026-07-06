using System.Runtime.Versioning;
using Microsoft.Win32;

namespace AraonMC.UI.Theme;

public static class SystemThemeHelper
{
    private const string ThemeRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeKey = "AppsUseLightTheme";

    [SupportedOSPlatform("windows")]
    public static bool IsSystemInDarkMode()
    {
        try
        {
            using var registryKey = Registry.CurrentUser.OpenSubKey(ThemeRegistryPath);
            if (registryKey is null) return false;
            var value = registryKey.GetValue(AppsUseLightThemeKey) as int?;
            return value == 0;
        }
        catch
        {
            return false;
        }
    }
}
