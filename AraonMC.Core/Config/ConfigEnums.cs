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

public class ConfigEnums
{
    /// <summary>
    /// 主题模式（亮/暗/系统）
    /// </summary>
    public enum ColorMode
    {
        Light = 0,
        Dark = 1,
        System = 2,
    }

    /// <summary>
    /// 配色主题
    /// </summary>
    public enum ColorTheme
    {
        SkyBlue = 0,
        Amber = 1,
    }
}