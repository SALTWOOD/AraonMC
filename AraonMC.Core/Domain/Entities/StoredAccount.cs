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

using AraonMC.Core.Domain.Enums;

namespace AraonMC.Core.Domain.Entities;

/// <summary>Persistence shape of an account — carries secrets, so it stays out of the UI layer.</summary>
public sealed class StoredAccount
{
    public string Uuid { get; set; } = string.Empty;

    public AccountType AccountType { get; set; }

    public string Username { get; set; } = string.Empty;

    /// <summary>Microsoft OAuth refresh token (Microsoft accounts only).</summary>
    public string? RefreshToken { get; set; }

    /// <summary>Third-party auth-server base URL (ThirdParty accounts only).</summary>
    public string? ServerUrl { get; set; }

    /// <summary>Raw <c>minecraft/profile</c> JSON (Microsoft accounts only).</summary>
    public string? ProfileJson { get; set; }
}
