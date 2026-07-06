namespace AraonMC.Core.Config;

/// <summary>
/// The root config catalog. The source generator reads the <see cref="ConfigCatalogAttribute"/>,
/// the nested <see cref="SectionAttribute"/> classes and their <see cref="KeyAttribute"/> partial
/// properties, and emits a typed, observable facade: <c>Config.General.Language</c>,
/// <c>Config.Java.MaxMemoryMb</c>, <c>Config.Instance.MaxMemoryMb[instance]</c>, etc.
/// </summary>
[ConfigCatalog]
public partial class Config
{
    [Section(Scope = ConfigScope.Global, Path = "general")]
    public sealed partial class GeneralSection
    {
        [Key(Default = "English (US)")] public partial string Language { get; set; }
        [Key(Default = true)]           public partial bool   KeepLauncherOpen { get; set; }
        [Key(Default = false)]          public partial bool   DiscordRpc { get; set; }
        [Key(Default = true)]           public partial bool   CheckUpdatesOnStart { get; set; }
    }

    [Section(Scope = ConfigScope.Global, Path = "java")]
    public sealed partial class JavaSection
    {
        [Key(Default = "")]                    public partial string JavaPath { get; set; }
        [Key(Default = "-Xmx4G -XX:+UseG1GC")] public partial string Arguments { get; set; }
        [Key(Default = 4096)]                  public partial int    MaxMemoryMb { get; set; }
        [Key(Default = 512)]                   public partial int    MinMemoryMb { get; set; }
    }

    [Section(Scope = ConfigScope.Global, Path = "game")]
    public sealed partial class GameSection
    {
        [Key(Default = "")]    public partial string GameDirectory { get; set; }
        [Key(Default = 1280)]  public partial int    WindowWidth { get; set; }
        [Key(Default = 720)]   public partial int    WindowHeight { get; set; }
        [Key(Default = false)] public partial bool   Fullscreen { get; set; }
    }

    [Section(Scope = ConfigScope.Global, Path = "theme")]
    public sealed partial class ThemeSection
    {
        [Key(Default = ConfigEnums.ColorMode.Dark)] public partial ConfigEnums.ColorMode ColorMode { get; set; }
        [Key(Default = ConfigEnums.ColorTheme.SkyBlue)] public partial ConfigEnums.ColorTheme DarkColor { get; set; }
        [Key(Default = ConfigEnums.ColorTheme.Amber)] public partial ConfigEnums.ColorTheme LightColor { get; set; }
    }

    /// <summary>
    /// Account-related global settings. Only scalars live here — the actual account records
    /// (with refresh tokens) are persisted by the dedicated <c>IAccountStore</c>, since they are
    /// a collection of secret-bearing rows outside this catalog's scalar scope.
    /// </summary>
    [Section(Scope = ConfigScope.Global, Path = "accounts")]
    public sealed partial class AccountSection
    {
        /// <summary>UUID of the active account, or empty if none is active.</summary>
        [Key(Default = "")] public partial string ActiveAccountId { get; set; }
    }

    /// <summary>
    /// Per-instance overrides keyed by each <see cref="Domain.Entities.MinecraftInstance"/>'s
    /// absolute path. A <c>null</c> value means "inherit the global default".
    /// </summary>
    [Section(Scope = ConfigScope.Instance, Path = "instance")]
    public sealed partial class InstanceSection
    {
        [Key(Default = null)] public partial InstanceKey<int?>    MaxMemoryMb { get; }
        [Key(Default = null)] public partial InstanceKey<string?> JavaPath { get; }
    }
}
