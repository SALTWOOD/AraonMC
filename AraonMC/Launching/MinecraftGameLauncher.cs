using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using AraonMC.Core.Application.Notifications;
using AraonMC.Core.Application.Ports;
using AraonMC.Core.Domain.Entities;
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
    private readonly INotificationService _notifications;

    public MinecraftGameLauncher(IAccountService accounts, INotificationService notifications)
    {
        _accounts = accounts;
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
        if (!File.Exists(versionJsonPath))
        {
            await WarnAsync("Version not installed",
                $"{instance.Name}: {versionId} is not downloaded yet. Install it from the Downloads page first.");
            return;
        }

        var java = ResolveJava();
        if (java is null)
        {
            await WarnAsync("Java not found",
                "Set the Java path in Settings, install Java, or define JAVA_HOME.");
            return;
        }

        try
        {
            // 在线账号需要有效 access token（刷新）；离线账号用 UUID 占位。
            string accessToken;
            if (account.IsOnline)
            {
                var token = await _accounts.GetAccessTokenAsync(account, ct);
                if (string.IsNullOrEmpty(token))
                {
                    await WarnAsync("Re-login required",
                        $"Please re-login {account.Username} before launching.");
                    return;
                }
                accessToken = token;
            }
            else
            {
                accessToken = account.Uuid;
            }

            var json = await File.ReadAllTextAsync(versionJsonPath, ct);
            var meta = VersionMetadataReader.Read(json);

            // 用户自定义 JVM 参数；剔除 -Xmx/-Xms，避免与下方按内存字段注入的冲突。
            var extraJvm = (CoreConfig.Java.Arguments ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(a => !a.StartsWith("-Xmx", StringComparison.Ordinal)
                         && !a.StartsWith("-Xms", StringComparison.Ordinal))
                .ToList();

            var context = new LaunchContext
            {
                Username = account.Username,
                Uuid = account.Uuid,
                AccessToken = accessToken,
                AccountKind = account.IsOnline ? AccountKind.Online : AccountKind.Offline,
                GameDirectory = instance.Path,
                VersionId = versionId,
                NativesDirectory = Path.Combine(versionDir, versionId + "-natives"),
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

            var command = new LaunchCommandBuilder().Build(meta, context);

            var psi = new ProcessStartInfo
            {
                FileName = command.JavaExecutable,
                UseShellExecute = false,
                WorkingDirectory = instance.Path,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            foreach (var arg in command.Arguments)
                psi.ArgumentList.Add(arg);

            var proc = Process.Start(psi);
            if (proc is null)
            {
                await WarnAsync("Launch failed", "Could not start the Java process.");
                return;
            }

            // 异步抽干 stdout/stderr，避免子进程写满管道阻塞；转 DebugLog 便于排查崩溃。
            proc.OutputDataReceived += (_, e) => { if (e.Data is not null) DebugLog.Info($"[mc] {e.Data}"); };
            proc.ErrorDataReceived += (_, e) => { if (e.Data is not null) DebugLog.Info($"[mc!] {e.Data}"); };
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            await _notifications.ShowAsync(NotificationRequest.Toast(
                "Launching", $"{instance.Name} is starting.", NotificationLevel.Info));
        }
        catch (Exception ex)
        {
            await WarnAsync("Launch failed", ex.Message);
        }
    }

    // ---- helpers ----

    /// <summary>Java 解析顺序：配置 JavaPath → JAVA_HOME/bin → PATH 上的 java。</summary>
    private static string? ResolveJava()
    {
        var configured = CoreConfig.Java.JavaPath;
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
            return configured;

        var exe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "java.exe" : "java";
        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrEmpty(javaHome))
        {
            var p = Path.Combine(javaHome, "bin", exe);
            if (File.Exists(p)) return p;
        }

        return exe; // 回退到 PATH（找不到由 Process.Start 报错）。
    }

    private async Task WarnAsync(string title, string body)
    {
        DebugLog.Info($"Launch: {title} — {body}");
        await _notifications.ShowAsync(NotificationRequest.Toast(title, body, NotificationLevel.Warning));
    }
}
