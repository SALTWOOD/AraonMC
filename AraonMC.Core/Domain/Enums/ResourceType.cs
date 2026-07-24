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

/// <summary>
/// A kind of browsable Minecraft content. Each platform maps a subset of these to its own notion of
/// project type / section (see <c>Infrastructure.Catalog.CatalogMappings</c>); types a platform does not
/// carry simply yield no results from that platform.
/// </summary>
public enum ResourceType
{
    Mod,
    Modpack,
    ResourcePack,
    ShaderPack,
    WorldSave,
    DataPack,
}
