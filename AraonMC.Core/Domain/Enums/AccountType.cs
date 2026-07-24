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

namespace AraonMC.Core.Domain.Enums;

/// <summary>How an account authenticates; determines the secret material in <see cref="Domain.Entities.StoredAccount"/>.</summary>
public enum AccountType
{
    /// <summary>Microsoft online ("正版验证"); six-step device-code flow, backed by an OAuth refresh token.</summary>
    Microsoft,

    /// <summary>authlib-injector / Yggdrasil ("第三方认证", e.g. LittleSkin). Not yet implemented.</summary>
    ThirdParty,

    /// <summary>Offline ("离线认证"); UUID derived from the username, no token.</summary>
    Offline
}
