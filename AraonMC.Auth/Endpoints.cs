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

internal static class Endpoints
{
    public const string DeviceCode = "https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode";

    public const string OAuthToken = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";

    public const string RefreshToken = "https://login.live.com/oauth20_token.srf";

    public const string XboxLiveAuthenticate = "https://user.auth.xboxlive.com/user/authenticate";

    public const string XstsAuthorize = "https://xsts.auth.xboxlive.com/xsts/authorize";

    public const string MinecraftLoginWithXbox =
        "https://api.minecraftservices.com/authentication/login_with_xbox";

    public const string Entitlements = "https://api.minecraftservices.com/entitlements/mcstore";

    public const string Profile = "https://api.minecraftservices.com/minecraft/profile";

    public const string Scope = "XboxLive.signin offline_access";

    public const string Tenant = "/consumers";
}
