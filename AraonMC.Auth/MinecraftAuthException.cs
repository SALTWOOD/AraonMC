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

namespace AraonMC.Auth;

/// <summary>
///     Minecraft 正版登录过程中可能出现的错误类型。
///     错误码 2148916xxx 对应 Xbox Live 的特定账户问题（参考 PrismarineJS/prismarine-auth）。
/// </summary>
public enum AuthErrorKind
{
    /// <summary>未归类的错误，详见 <see cref="System.Exception.Message" />。</summary>
    Unknown,

    /// <summary>设备码已过期，用户未在有效期内完成授权。</summary>
    DeviceCodeExpired,

    /// <summary>设备码轮询多次失败。</summary>
    DeviceCodePollingFailed,

    /// <summary>refresh token 已失效，需要重新登录（invalid_grant 等）。</summary>
    ReloginRequired,

    /// <summary>账户被 Xbox Live 封禁（2148916227）。</summary>
    XboxBanned,

    /// <summary>账户尚未注册 Xbox 档案（2148916233）。</summary>
    XboxNotRegistered,

    /// <summary>账户所在地区受限制（2148916235）。</summary>
    RegionBlocked,

    /// <summary>账户为未成年，需成人协助（2148916238）。</summary>
    Underage,

    /// <summary>账户未持有 Minecraft（Step 5 所有权校验未通过）。</summary>
    NotPurchased,

    /// <summary>账户尚未创建 Minecraft 玩家档案（Step 6 返回 404）。</summary>
    ProfileNotCreated,

    /// <summary>请求过于频繁（429）。</summary>
    TooManyRequests,

    /// <summary>IP 异常 / 风控（403）。</summary>
    AbnormalIp
}

/// <summary>
///     Minecraft 正版登录失败时抛出的异常，携带结构化的错误类型与可选的帮助链接。
/// </summary>
public sealed class MinecraftAuthException : Exception
{
    public AuthErrorKind Kind { get; }

    /// <summary>针对该错误的帮助页面 URL（如注册 Xbox、修改出生日期、购买 MC 等），可能为空。</summary>
    public string? HelpUrl { get; }

    public MinecraftAuthException(AuthErrorKind kind, string message, string? helpUrl = null,
        Exception? inner = null)
        : base(message, inner)
    {
        Kind = kind;
        HelpUrl = helpUrl;
    }
}
