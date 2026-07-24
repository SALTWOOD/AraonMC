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

namespace AraonMC.Core.Config;

/// <summary>
/// The storage scope a config key belongs to. Determines which file the value
/// is read from / written to.
/// </summary>
public enum ConfigScope
{
    /// <summary>
    /// Shared application-wide settings, stored in the fixed OS config location
    /// (e.g. <c>%APPDATA%\AraonMC\config.toml</c>).
    /// </summary>
    Global,

    /// <summary>
    /// Per-instance overrides, keyed by the instance's absolute filesystem path
    /// (stored in <c>instances.toml</c>).
    /// </summary>
    Instance,
}
