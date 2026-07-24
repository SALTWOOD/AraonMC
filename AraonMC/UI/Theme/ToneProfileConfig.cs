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

public record ToneProfileConfig
{
    public static readonly ToneProfile DefaultLight = new(
        L1: 0.28,  L2: 0.44,  L3: 0.56,  L4: 0.68,
        L5: 0.95,  L6: 0.90,  L7: 0.84,  L8: 0.78,
        LBackground: 0.93
    );

    public static readonly ToneProfile DefaultDark = new(
        L1: 0.85,  L2: 0.65,  L3: 0.48,  L4: 0.32,
        L5: 0.26,  L6: 0.22,  L7: 0.17,  L8: 0.15,
        LBackground: 0.18, LForeground: 1, LWhite: 0.275
    );

    public ToneProfile Light { get => field ?? DefaultLight; init; } = null!;

    public ToneProfile Dark { get => field ?? DefaultDark; init; } = null!;
}
