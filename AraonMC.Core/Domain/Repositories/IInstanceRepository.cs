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

using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Enums;

namespace AraonMC.Core.Domain.Repositories;

/// <summary>
/// Local game-instance store (repository contract). Real persistence backend
/// is not implemented yet — see the Stub implementation in Infrastructure.
/// </summary>
public interface IInstanceRepository
{
    IReadOnlyList<GameInstance> GetAll();

    Task<GameInstance> CreateAsync(string name, MinecraftVersion version, LoaderType loader, CancellationToken ct = default);
    Task SaveAsync(GameInstance instance, CancellationToken ct = default);
    Task RenameAsync(GameInstance instance, string newName, CancellationToken ct = default);
    Task DeleteAsync(GameInstance instance, CancellationToken ct = default);
}
