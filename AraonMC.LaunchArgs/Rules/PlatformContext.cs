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

using System.Runtime.InteropServices;

namespace AraonMC.LaunchArgs.Rules;

/// <summary>当前运行平台快照，用于判定 OS 条件。可显式构造用于测试。</summary>
public sealed class PlatformContext
{
    public OperatingSystemKind OperatingSystem { get; init; }
    public string Version { get; init; } = string.Empty;
    public string Arch { get; init; } = string.Empty;

    public static PlatformContext Current { get; } = Detect();

    private static PlatformContext Detect()
    {
        var os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? OperatingSystemKind.Windows
               : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? OperatingSystemKind.OSX
               : OperatingSystemKind.Linux;

        var arch = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X86 => "x86",
            Architecture.X64 => "x64",
            Architecture.Arm => "arm",
            Architecture.Arm64 => "arm64",
            _ => RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant(),
        };

        var ver = os == OperatingSystemKind.Windows
            ? Environment.OSVersion.Version.ToString()
            : "0"; // TODO: macOS / Linux 真实版本探测

        return new PlatformContext { OperatingSystem = os, Arch = arch, Version = ver };
    }
}
