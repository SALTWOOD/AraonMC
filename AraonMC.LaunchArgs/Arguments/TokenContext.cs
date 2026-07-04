using System.Text.RegularExpressions;

namespace AraonMC.LaunchArgs.Arguments;

/// <summary><c>${...}</c> 占位符取值表。</summary>
public sealed class TokenContext
{
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

    public TokenContext Set(string token, string value)
    {
        _values[token] = value;
        return this;
    }

    public string? Get(string token) => _values.TryGetValue(token, out var v) ? v : null;

    /// <summary>替换模板里的 <c>${name}</c>；未知占位符原样保留。</summary>
    public string Substitute(string template)
    {
        if (string.IsNullOrEmpty(template)) return template ?? string.Empty;

        return Regex.Replace(template, @"\$\{(?<name>[^}]+)\}", m =>
            _values.TryGetValue(m.Groups["name"].Value, out var v) ? v : m.Value);
    }

    /// <summary>用 <see cref="LaunchContext"/> 填充标准占位符（classpath、main_class 除外）。</summary>
    public static TokenContext FromLaunchContext(LaunchContext c)
    {
        var t = new TokenContext()
            .Set("auth_player_name", c.Username)
            .Set("auth_uuid", c.Uuid)
            .Set("auth_access_token", c.AccessToken)
            .Set("auth_session", c.AccessToken)
            .Set("user_type", c.AccountKind == AccountKind.Online ? "mojang" : "legacy")
            .Set("version_name", c.VersionId)
            .Set("version_type", c.VersionType)
            .Set("game_directory", c.GameDirectory)
            .Set("assets_root", c.AssetsRoot)
            .Set("assets_index_name", c.AssetsIndexName)
            .Set("natives_directory", c.NativesDirectory)
            .Set("launcher_name", c.LauncherName)
            .Set("launcher_version", c.LauncherVersion)
            .Set("classpath_separator", Path.PathSeparator.ToString())
            .Set("library_directory", c.LibrariesDirectory)
            .Set("client_jar", c.ClientJarPath);

        if (c.ResolutionWidth.HasValue) t.Set("resolution_width", c.ResolutionWidth.Value.ToString());
        if (c.ResolutionHeight.HasValue) t.Set("resolution_height", c.ResolutionHeight.Value.ToString());

        return t;
    }
}
