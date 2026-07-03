namespace AraonMC.Auth;

/// <summary>
///     Minecraft 正版登录的配置选项。
/// </summary>
public sealed class MinecraftAuthOptions
{
    /// <summary>
    ///     微软 OAuth 应用的 Client ID（public client，无需 secret）。
    ///     原启动器从编译期注入的 <c>Secrets.MSOAuthClientId</c> 读取，提取后改为由调用方显式提供。
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    ///     设备码登录时的 UI 实现（必需）。本库不再附带控制台实现，调用方需提供自己的
    ///     <see cref="IDeviceCodeUI" />（如桌面 GUI 弹窗）。
    /// </summary>
    public required IDeviceCodeUI DeviceCodeUI { get; init; }

    /// <summary>
    ///     可选的日志回调，用于输出每一步的进度信息（对应原版的 <c>ModProfile.ProfileLog</c>）。
    /// </summary>
    public Action<string>? Logger { get; init; }

    /// <summary>单次 HTTP 请求的超时时间，默认 30 秒。</summary>
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     可注入的自定义 <see cref="HttpClient" />。为 <c>null</c> 时模块内部创建一个实例。
    /// </summary>
    public HttpClient? HttpClient { get; init; }

    /// <summary>
    ///     access token 的内存缓存有效期，默认 10 分钟（对齐原版 <c>mcLoginMsRefreshTime</c> 的 10 分钟阈值）。
    ///     设为 <c>null</c> 可关闭缓存（多账户场景建议关闭，避免把 A 账户的 token 误用到 B 账户）。
    /// </summary>
    public TimeSpan? AccessTokenCacheTtl { get; init; } = TimeSpan.FromMinutes(10);
}
