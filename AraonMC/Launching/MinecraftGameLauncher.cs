using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using AraonMC.Core.Application.Notifications;
using AraonMC.Core.Application.Ports;
using AraonMC.Core.Domain.Entities;
using AraonMC.Downloads;
using AraonMC.LaunchArgs;
using AraonMC.LaunchArgs.Version;
// Alias: the app's Config/ folder exposes namespace AraonMC.Config, which would shadow the
// generated facade under the bare name `Config`.
using CoreConfig = AraonMC.Core.Config.Config;

namespace AraonMC.Launching;

/// <summary>
///   用 <see cref="LaunchCommandBuilder"/> 把版本 json + 账号 + 配置组装成 java 启动命令并拉起进程。
///   账号 token 通过 <see cref="IAccountService"/> 在线刷新；离线账号用 UUID 占位。
///   目前仅支持 Vanilla（无 InheritsFrom 继承链；VersionMerger 尚未实现）。
///   所有失败路径内部以通知上报并正常返回——本方法不向上抛。
/// </summary>
public sealed class MinecraftGameLauncher : IGameLauncher
{
    private readonly IAccountService _accounts;
    private readonly NativeLibraryExtractor _natives;
    private readonly INotificationService _notifications;

    public MinecraftGameLauncher(
        IAccountService accounts,
        NativeLibraryExtractor natives,
        INotificationService notifications)
    {
        _accounts = accounts;
        _natives = natives;
        _notifications = notifications;
    }

    public async Task LaunchAsync(GameInstance instance, MinecraftAccount account, CancellationToken ct = default)
    {
        if (account is null)
        {
            await WarnAsync("No account selected", "Add or select an account before launching.");
            return;
        }

        var versionId = instance.MinecraftVersion;
        var versionDir = Path.Combine(instance.Path, "versions", versionId);
        var versionJsonPath = Path.Combine(versionDir, versionId + ".json");
        DebugLog.Info($"Launch: requested for instance '{instance.Name}' (id={instance.Id}, version='{versionId}', base='{instance.BaseMinecraftVersion}', loader={instance.Loader}, gameDir='{instance.Path}').");
        DebugLog.Info($"Launch: account='{account.Username}' (uuid={account.Uuid}, type={account.AccountType}, online={account.IsOnline}).");

        if (!File.Exists(versionJsonPath))
        {
            await WarnAsync("Version not installed",
                $"{instance.Name}: {versionId} is not downloaded yet. Install it from the Downloads page first.");
            return;
        }
        DebugLog.Info($"Launch: version metadata json found at '{versionJsonPath}'.");

        var java = ResolveJava();
        if (java is null)
        {
            await WarnAsync("Java not found",
                "Set the Java path in Settings, install Java, or define JAVA_HOME.");
            return;
        }
        DebugLog.Info($"Launch: resolved Java executable → '{java}'.");

        try
        {
            // 在线账号需要有效 access token（刷新）；离线账号用 UUID 占位。
            string accessToken;
            if (account.IsOnline)
            {
                DebugLog.Info($"Launch: online account — requesting a fresh access token for '{account.Username}'.");
                var token = await _accounts.GetAccessTokenAsync(account, ct);
                if (string.IsNullOrEmpty(token))
                {
                    await WarnAsync("Re-login required",
                        $"Please re-login {account.Username} before launching.");
                    return;
                }
                accessToken = token;
                DebugLog.Info($"Launch: access token obtained ({token.Length} char(s); value not logged).");
            }
            else
            {
                accessToken = account.Uuid;
                DebugLog.Info("Launch: offline account — using the UUID as the access-token placeholder.");
            }

            var json = await File.ReadAllTextAsync(versionJsonPath, ct);
            var meta = VersionMetadataReader.Read(json);
            DebugLog.Info($"Launch: parsed version metadata — id='{meta.Id}', mainClass='{meta.MainClass}', type='{meta.Type}', assetsIndex='{meta.AssetsIndexName}', libraries={meta.Libraries.Count}, inheritsFrom='{meta.InheritsFrom ?? "(none)"}'.");

            // 现代 MC（LWJGL 3）约定：natives 是启动期临时产物，解压到系统临时目录，
            // 每次启动重建，不常驻 .minecraft。安装期已把 native jar 下到 libraries/。
            var nativesDir = Path.Combine(Path.GetTempPath(), "AraonMC", "natives", instance.Id);
            DebugLog.Info($"Launch: extracting natives to temp dir '{nativesDir}'.");
            await Task.Run(() => _natives.ExtractTo(instance.Path, versionId, nativesDir), ct);

            // 用户自定义 JVM 参数；剔除 -Xmx/-Xms，避免与下方按内存字段注入的冲突。
            var extraJvm = (CoreConfig.Java.Arguments ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(a => !a.StartsWith("-Xmx", StringComparison.Ordinal)
                         && !a.StartsWith("-Xms", StringComparison.Ordinal))
                .ToList();
            DebugLog.Info($"Launch: JVM config — minMem={CoreConfig.Java.MinMemoryMb}MB, maxMem={CoreConfig.Java.MaxMemoryMb}MB, javaArgs='{CoreConfig.Java.Arguments}', effective extra JVM args ({extraJvm.Count})=[{string.Join(' ', extraJvm)}].");

            var context = new LaunchContext
            {
                Username = account.Username,
                Uuid = account.Uuid,
                AccessToken = accessToken,
                AccountKind = account.IsOnline ? AccountKind.Online : AccountKind.Offline,
                GameDirectory = instance.Path,
                VersionId = versionId,
                NativesDirectory = nativesDir,
                LibrariesDirectory = Path.Combine(instance.Path, "libraries"),
                AssetsRoot = Path.Combine(instance.Path, "assets"),
                ClientJarPath = Path.Combine(versionDir, versionId + ".jar"),
                AssetsIndexName = meta.AssetsIndexName,
                JavaExecutable = java,
                MinMemoryMb = CoreConfig.Java.MinMemoryMb,
                MaxMemoryMb = CoreConfig.Java.MaxMemoryMb,
                VersionType = "AraonMC",
                ExtraJvmArguments = extraJvm,
            };
            DebugLog.Info($"Launch: context assembled — versionId='{context.VersionId}', natives='{context.NativesDirectory}', clientJar='{context.ClientJarPath}', accountKind={context.AccountKind}.");

            var command = new LaunchCommandBuilder().Build(meta, context);
            DebugLog.Info($"Launch: launch command built — mainClass='{command.MainClass}', jvmArgs={command.JvmArguments.Count}, gameArgs={command.GameArguments.Count}.");

            var psi = new ProcessStartInfo
            {
                FileName = command.JavaExecutable,
                UseShellExecute = false,
                WorkingDirectory = instance.Path,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            var args = command.Arguments.ToList();
            foreach (var arg in args)
                psi.ArgumentList.Add(arg);
            DebugLog.Info($"Launch: starting Java process (workingDir='{psi.WorkingDirectory}', {args.Count} arg(s)).");
            DebugLog.Info($"Launch: command line (access token redacted): {command.JavaExecutable} {RedactArgs(args)}");

            var proc = Process.Start(psi);
            if (proc is null)
            {
                await WarnAsync("Launch failed", "Could not start the Java process.");
                return;
            }

            DebugLog.Info($"Launch: Java process started (pid={proc.Id}).");

            // 监听退出，记录 exitCode 便于排查游戏崩溃。
            proc.EnableRaisingEvents = true;
            proc.Exited += (_, _) =>
            {
                try { DebugLog.Info($"Launch: Java process exited (pid={proc.Id}, exitCode={proc.ExitCode})."); }
                catch (Exception ex) { DebugLog.Warn($"Launch: failed to read process exit code — {ex.Message}."); }
            };

            // 异步抽干 stdout/stderr，避免子进程写满管道阻塞；转 DebugLog 便于排查崩溃。
            proc.OutputDataReceived += (_, e) => { if (e.Data is not null) DebugLog.Info($"[mc] {e.Data}"); };
            proc.ErrorDataReceived += (_, e) => { if (e.Data is not null) DebugLog.Info($"[mc!] {e.Data}"); };
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            await _notifications.ShowAsync(NotificationRequest.Toast(
                "Launching", $"{instance.Name} is starting.", NotificationLevel.Info));
            DebugLog.Info($"Launch: '{instance.Name}' handoff complete; the game process is now running.");
        }
        catch (Exception ex)
        {
            DebugLog.Error($"Launch: failed to launch '{instance.Name}' — {ex.GetType().Name}: {ex.Message}");
            await WarnAsync("Launch failed", ex.Message);
        }
    }

    // ---- helpers ----

    /// <summary>Java 解析顺序：配置 JavaPath → JAVA_HOME/bin → PATH 上的 java。</summary>
    private static string? ResolveJava()
    {
        var configured = CoreConfig.Java.JavaPath;
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
        {
            DebugLog.Info($"Launch: Java resolved from configured JavaPath → '{configured}'.");
            return configured;
        }
        if (!string.IsNullOrWhiteSpace(configured))
            DebugLog.Warn($"Launch: configured JavaPath '{configured}' does not exist; falling back.");

        var exe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "java.exe" : "java";
        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrEmpty(javaHome))
        {
            var p = Path.Combine(javaHome, "bin", exe);
            if (File.Exists(p))
            {
                DebugLog.Info($"Launch: Java resolved from JAVA_HOME ('{javaHome}') → '{p}'.");
                return p;
            }
            DebugLog.Warn($"Launch: JAVA_HOME='{javaHome}' but '{p}' not found; falling back to PATH.");
        }
        else
        {
            DebugLog.Info("Launch: JAVA_HOME not set; falling back to java on PATH.");
        }

        DebugLog.Info($"Launch: Java resolved from PATH → '{exe}' (will fail at Process.Start if not present).");
        return exe; // 回退到 PATH（找不到由 Process.Start 报错）。
    }

    /// <summary>Renders the argument list with the value following <c>--accessToken</c> masked, so the
    /// command line can be logged without leaking the Minecraft session token.</summary>
    private static string RedactArgs(IReadOnlyList<string> args)
    {
        const string tokenFlag = "--accessToken";
        var redacted = new List<string>(args.Count);
        for (var i = 0; i < args.Count; i++)
        {
            if (args[i] == tokenFlag && i + 1 < args.Count)
            {
                redacted.Add(tokenFlag);
                redacted.Add("<redacted>");
                i++; // skip the token value
            }
            else
            {
                redacted.Add(args[i]);
            }
        }
        return string.Join(' ', redacted);
    }

    private async Task WarnAsync(string title, string body)
    {
        DebugLog.Warn($"Launch: {title} — {body}");
        await _notifications.ShowAsync(NotificationRequest.Toast(title, body, NotificationLevel.Warning));
    }
}
