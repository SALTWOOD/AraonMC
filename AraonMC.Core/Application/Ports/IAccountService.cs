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

using System.Collections.ObjectModel;
using AraonMC.Core.Domain.Entities;

namespace AraonMC.Core.Application.Ports;

/// <summary>Account login / identity management (application port).</summary>
public interface IAccountService
{
    /// <summary>
    /// Live, service-owned account list — bind directly; both the Accounts page and the top-bar
    /// switcher share this instance.
    /// </summary>
    ObservableCollection<MinecraftAccount> Accounts { get; }

    MinecraftAccount? GetActive();

    Task<MinecraftAccount> LoginMicrosoftAsync(CancellationToken ct = default);

    Task<MinecraftAccount> AddOfflineAsync(string username, CancellationToken ct = default);

    Task SetActiveAsync(MinecraftAccount account, CancellationToken ct = default);

    Task RemoveAsync(MinecraftAccount account, CancellationToken ct = default);

    /// <summary>A live Minecraft access token for launching, or null if re-authentication is needed.</summary>
    Task<string?> GetAccessTokenAsync(MinecraftAccount account, CancellationToken ct = default);
}
