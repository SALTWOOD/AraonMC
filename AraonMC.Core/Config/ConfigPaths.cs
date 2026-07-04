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
    /// 游戏根目录（.minecraft）：取 config 配置值，为空时默认 <c>&lt;GlobalRoot&gt;/.minecraft</c>
    /// （Windows 上即 <c>%APPDATA%\AraonMC\.minecraft</c>）。
    /// </summary>
    public static string GameDirectory()
    {
        var dir = Config.Game.GameDirectory;
        return !string.IsNullOrWhiteSpace(dir) ? dir : Path.Combine(GlobalRoot(), ".minecraft");
    }
}
