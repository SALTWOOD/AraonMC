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

using System;
using System.IO;

namespace AraonMC.Core.Config;

/// <summary>
/// Resolves the native, per-OS locations for AraonMC's config files. The launcher writes
/// nothing next to its own binary (green/portable); all state lives under <see cref="GlobalRoot"/>.
/// </summary>
public static class ConfigPaths
{
    /// <summary>
    /// The OS-native root directory holding both <c>config.toml</c> and <c>instances.toml</c>:
    /// <list type="bullet">
    /// <item>Windows: <c>%APPDATA%\AraonMC</c></item>
    /// <item>macOS: <c>~/Library/Application Support/AraonMC</c></item>
    /// <item>Linux: <c>$XDG_CONFIG_HOME/araonmc</c> (fallback <c>~/.config/araonmc</c>)</item>
    /// </list>
    /// </summary>
    public static string GlobalRoot()
    {
        if (OperatingSystem.IsWindows())
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AraonMC");

        if (OperatingSystem.IsMacOS())
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library", "Application Support", "AraonMC");

        // Linux and other Unix-likes: XDG_CONFIG_HOME, default ~/.config
        var xdg = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        var @base = !string.IsNullOrEmpty(xdg)
            ? xdg
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        return Path.Combine(@base, "araonmc");
    }

    public static string GlobalConfigFile() => Path.Combine(GlobalRoot(), "config.toml");

    public static string InstancesConfigFile() => Path.Combine(GlobalRoot(), "instances.toml");

    /// <summary>
    /// Minecraft 游戏目录（.minecraft）的系统标准位置（与官方启动器一致）：
    /// <list type="bullet">
    /// <item>Windows: <c>%APPDATA%\.minecraft</c></item>
    /// <item>macOS: <c>~/Library/Application Support/minecraft</c></item>
    /// <item>Linux: <c>~/.minecraft</c></item>
    /// </list>
    /// </summary>
    public static string DefaultGameDirectory()
    {
        if (OperatingSystem.IsWindows())
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");

        if (OperatingSystem.IsMacOS())
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library", "Application Support", "minecraft");

        // Linux and other Unix-likes
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".minecraft");
    }

    /// <summary>
    /// 游戏根目录：取 config 配置值，为空时回退到 <see cref="DefaultGameDirectory"/>（系统标准位置）。
    /// </summary>
    public static string GameDirectory()
    {
        var dir = Config.Game.GameDirectory;
        return !string.IsNullOrWhiteSpace(dir) ? dir : DefaultGameDirectory();
    }
}
