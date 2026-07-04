using System.IO;
using AraonMC.LaunchArgs.Arguments;
using AraonMC.LaunchArgs.Libraries;
using AraonMC.LaunchArgs.Rules;
using AraonMC.LaunchArgs.Version;

namespace AraonMC.LaunchArgs;

/// <summary>把 <see cref="VersionMetadata"/> + <see cref="LaunchContext"/> 组装成 <see cref="LaunchCommand"/>。</summary>
public sealed class LaunchCommandBuilder
{
    private readonly PlatformContext _platform;
    private readonly RuleEvaluator _rules;
    private readonly ArgumentExpander _expander;
    private readonly ClasspathResolver _classpath;

    public LaunchCommandBuilder(PlatformContext? platform = null)
    {
        _platform = platform ?? PlatformContext.Current;
        _rules = new RuleEvaluator(_platform);
        _expander = new ArgumentExpander(_rules);
        _classpath = new ClasspathResolver(_rules, _platform);
    }

    public LaunchCommand Build(VersionMetadata version, LaunchContext context)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(context);

        // 特性集
        var features = new HashSet<string>();
        if (context.IsDemoUser) features.Add("is_demo_user");
        if (context.ResolutionWidth.HasValue || context.ResolutionHeight.HasValue)
            features.Add("has_custom_resolution");

        // classpath
        var resolved = _classpath.Resolve(version.Libraries, features);
        var classpathEntries = new List<string>();
        foreach (var r in resolved)
        {
            if (!string.IsNullOrEmpty(r.ClasspathPath))
                classpathEntries.Add(Path.Combine(context.LibrariesDirectory, r.ClasspathPath));
        }
        if (!string.IsNullOrEmpty(context.ClientJarPath))
            classpathEntries.Add(context.ClientJarPath);
        var classpath = string.Join(Path.PathSeparator, classpathEntries);

        // 占位符
        var tokens = TokenContext.FromLaunchContext(context);
        tokens.Set("classpath", classpath);

        // JVM 参数
        var jvmEntries = version.JvmArguments.Count > 0 ? version.JvmArguments : _expander.DefaultJvmArguments();
        var jvmArgs = new List<string>(_expander.Expand(jvmEntries, features));

        var memPrefix = new List<string>();
        if (context.MaxMemoryMb.HasValue) memPrefix.Add($"-Xmx{context.MaxMemoryMb.Value}M");
        if (context.MinMemoryMb.HasValue) memPrefix.Add($"-Xms{context.MinMemoryMb.Value}M");
        jvmArgs.InsertRange(0, memPrefix);

        jvmArgs.AddRange(context.ExtraJvmArguments);

        for (var i = 0; i < jvmArgs.Count; i++)
            jvmArgs[i] = tokens.Substitute(jvmArgs[i]);

        // 游戏参数
        List<string> gameArgs = !string.IsNullOrEmpty(version.LegacyMinecraftArguments)
            ? _expander.ExpandLegacy(version.LegacyMinecraftArguments!).ToList()
            : _expander.Expand(version.GameArguments, features).ToList();

        for (var i = 0; i < gameArgs.Count; i++)
            gameArgs[i] = tokens.Substitute(gameArgs[i]);

        // TODO: 注入 logging 配置参数（version.Logging.Client.Argument）。

        return new LaunchCommand
        {
            JavaExecutable = context.JavaExecutable,
            JvmArguments = jvmArgs,
            MainClass = version.MainClass,
            GameArguments = gameArgs,
        };
    }
}
