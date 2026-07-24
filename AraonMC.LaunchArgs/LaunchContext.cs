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

/// <summary>账户类型，决定 <c>user_type</c> 取值。</summary>
public enum AccountKind
{
    Online,
    Offline,
    Legacy,
}

/// <summary>
/// 构建启动命令所需的运行时输入。由主项目从领域对象映射而来，保持本库零依赖、可独立测试。
/// </summary>
public sealed class LaunchContext
{
    public string Username { get; set; } = string.Empty;
    public string Uuid { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public AccountKind AccountKind { get; set; } = AccountKind.Online;

    public string GameDirectory { get; set; } = string.Empty;
    public string NativesDirectory { get; set; } = string.Empty;
    public string LibrariesDirectory { get; set; } = string.Empty;
    public string AssetsRoot { get; set; } = string.Empty;
    public string ClientJarPath { get; set; } = string.Empty;

    public string VersionId { get; set; } = string.Empty;

    /// <summary>启动器品牌化的版本类型，默认 release，可填自定义标识。</summary>
    public string VersionType { get; set; } = "release";

    public string AssetsIndexName { get; set; } = string.Empty;

    public string JavaExecutable { get; set; } = string.Empty;
    public int? MinMemoryMb { get; set; }
    public int? MaxMemoryMb { get; set; }

    public bool IsDemoUser { get; set; }
    public int? ResolutionWidth { get; set; }
    public int? ResolutionHeight { get; set; }

    public string LauncherName { get; set; } = "AraonMC";
    public string LauncherVersion { get; set; } = string.Empty;
    public IReadOnlyList<string> ExtraJvmArguments { get; set; } = [];
}
