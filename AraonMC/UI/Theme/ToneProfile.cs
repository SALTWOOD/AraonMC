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

namespace AraonMC.UI.Theme;

public record ToneProfile(
    double L1 = 0.35,
    double L2 = 0.5,
    double L3 = 0.575,
    double L4 = 0.65,
    double L5 = 0.8,
    double L6 = 0.92,
    double L7 = 0.94,
    double L8 = 0.96,
    double LWhite = 1,
    double LForeground = 0,
    double LBackground = 0.995,
    double C1 = 0.025,
    double C2 = 0.188,
    double C3 = 0.213,
    double C4 = 0.168,
    double C5 = 0.093,
    double C6 = 0.036,
    double C7 = 0.028,
    double C8 = 0.018,
    double ASemiWhite = 0.733,
    double AHalfWhite = 0.333,
    double ASemiTransparent = 0.004,
    double AHalfTransparent = 0.5,
    double ATransparent = 0,
    double ABackground = 0.824,
    double AToolTip = 0.9
);
