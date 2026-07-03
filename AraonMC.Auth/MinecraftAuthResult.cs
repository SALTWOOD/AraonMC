namespace AraonMC.Auth;

/// <summary>
///     Minecraft 正版登录的返回结果。包含启动游戏所需的全部信息，调用方负责持久化。
/// </summary>
public sealed class MinecraftAuthResult
{
    /// <summary>玩家 UUID（无连字符的小写 32 位十六进制）。</summary>
    public required string Uuid { get; init; }

    /// <summary>玩家用户名（IGN）。</summary>
    public required string Username { get; init; }

    /// <summary>Minecraft access token，用于启动游戏时的身份认证。</summary>
    public required string AccessToken { get; init; }

    /// <summary>
    ///     微软 OAuth refresh token，用于下次启动时刷新 access token，避免再次扫码。
    /// </summary>
    public required string RefreshToken { get; init; }

    /// <summary>
    ///     <c>api.minecraftservices.com/minecraft/profile</c> 返回的原始 JSON，包含皮肤、档案等完整信息。
    /// </summary>
    public required string ProfileJson { get; init; }
}
