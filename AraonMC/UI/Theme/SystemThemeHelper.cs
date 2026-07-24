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
