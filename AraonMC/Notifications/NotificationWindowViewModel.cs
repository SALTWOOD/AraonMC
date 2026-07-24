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

using AraonMC.Core.Application.Notifications;
using AraonMC.ViewModels;

namespace AraonMC.Notifications;

/// <summary>
/// Presentation model for a single <see cref="NotificationWindow"/>.
/// Level → color/glyph mapping is handled in XAML via <c>NotificationLevelConverter</c>;
/// this view model only exposes plain display data.
/// </summary>
public sealed class NotificationWindowViewModel : ViewModelBase
{
    internal NotificationWindowViewModel(NotificationRequest request)
    {
        Request = request;
        Title = request.Title;
        Message = request.Message;
        Level = request.Level;
    }

    public NotificationRequest Request { get; }
    public string Title { get; }
    public string Message { get; }
    public NotificationLevel Level { get; }

    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
}
