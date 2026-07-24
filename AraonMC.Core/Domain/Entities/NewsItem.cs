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

namespace AraonMC.Core.Domain.Entities;

/// <summary>
/// A news / update entry shown on the Home page.
/// </summary>
public sealed class NewsItem
{
    public string Title { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; }
    public string? Link { get; set; }
}
