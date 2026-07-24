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

using Avalonia;
using Avalonia.Media;

namespace AraonMC.UI.Theme;

public class ColorResource
{
    public required byte A { get; init; }
    public required byte R { get; init; }
    public required byte G { get; init; }
    public required byte B { get; init; }
    public required string Suffix { get; init; }
    public bool ApplyToBrush { get; init; } = true;
    public bool ApplyToColor { get; init; } = true;

    public Color ToColor() => Color.FromArgb(A, R, G, B);

    public SolidColorBrush ToBrush() => new(ToColor());

    public void Apply()
    {
        var res = Application.Current!.Resources;
        var color = ToColor();
        if (ApplyToColor) res[$"ColorObject{Suffix}"] = color;
        if (ApplyToBrush) res[$"ColorBrush{Suffix}"] = new SolidColorBrush(color);
    }

    public void Apply(string colorKey, string brushKey)
    {
        var res = Application.Current!.Resources;
        var color = ToColor();
        res[colorKey] = color;
        res[brushKey] = new SolidColorBrush(color);
    }
}
