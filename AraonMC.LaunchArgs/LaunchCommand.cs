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

namespace AraonMC.LaunchArgs;

/// <summary>构建出的启动命令。由调用方用 <c>Process</c> 执行，本库不拉起进程。</summary>
public sealed class LaunchCommand
{
    public string JavaExecutable { get; init; } = string.Empty;
    public IReadOnlyList<string> JvmArguments { get; init; } = [];
    public string MainClass { get; init; } = string.Empty;
    public IReadOnlyList<string> GameArguments { get; init; } = [];

    /// <summary>java 之后的全部参数，可直接喂给 <c>ProcessStartInfo.ArgumentList</c>。</summary>
    public IEnumerable<string> Arguments
    {
        get
        {
            foreach (var a in JvmArguments) yield return a;
            yield return MainClass;
            foreach (var a in GameArguments) yield return a;
        }
    }

    public override string ToString() => $"{JavaExecutable} {string.Join(' ', Arguments)}";
}
