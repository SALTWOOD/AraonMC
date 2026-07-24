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
/// The application port behind the source-generated <c>Config</c> facade. Reads and writes
/// individual scalar/enum key values, optionally scoped to a specific instance path.
/// </summary>
/// <remarks>
/// The concrete file-backed implementation lives in the presentation layer; tests inject an
/// in-memory implementation. <see cref="Config"/> (source-generated) delegates every accessor
/// to the single store installed via <c>Config.Initialize</c> at startup.
/// </remarks>
public interface IConfigStore
{
    /// <summary>Reads the value at <paramref name="path"/>, or returns <paramref name="defaultValue"/>.</summary>
    T Get<T>(ConfigScope scope, string path, T defaultValue, string? instancePath = null);

    /// <summary>Writes <paramref name="value"/> to <paramref name="path"/> (write-through, atomic).</summary>
    void Set<T>(ConfigScope scope, string path, T value, string? instancePath = null);
}
