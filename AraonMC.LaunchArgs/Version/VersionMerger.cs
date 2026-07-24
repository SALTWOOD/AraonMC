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

namespace AraonMC.LaunchArgs.Version;

/// <summary>合并 <see cref="VersionMetadata.InheritsFrom"/> 继承链（模组版本继承原版）。</summary>
public static class VersionMerger
{
    // TODO: libraries 同名子覆盖父，arguments 先父后子拼接，标量子覆盖。

    /// <param name="chain">从根父版本到目标版本的有序链。</param>
    public static VersionMetadata Merge(IReadOnlyList<VersionMetadata> chain)
        => throw new NotImplementedException();
}
