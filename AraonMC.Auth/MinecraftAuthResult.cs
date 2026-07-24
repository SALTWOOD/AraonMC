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
