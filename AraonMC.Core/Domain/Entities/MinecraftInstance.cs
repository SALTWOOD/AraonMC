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

namespace AraonMC.Core.Domain.Entities;

/// <summary>
/// A real, on-disk Minecraft installation located at <see cref="Path"/>. This is the identity
/// used by the config system — per-instance settings are keyed by this absolute path. It will
/// grow to carry more launch-relevant state. Distinct from <see cref="GameInstance"/>, which is
/// the frontend display DTO.
/// </summary>
public sealed class MinecraftInstance
{
    /// <summary>Absolute filesystem path of the instance's <c>.minecraft</c> directory.</summary>
    public string Path { get; set; } = string.Empty;
}
