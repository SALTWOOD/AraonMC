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

namespace AraonMC.Core.Application.Notifications;

/// <summary>
/// How a notification is presented.
/// <list type="bullet">
/// <item><see cref="Blocking"/>: shown as a modal dialog. Only one is visible at a time;
/// additional blocking notifications queue and are displayed one-by-one as the user dismisses each.</item>
/// <item><see cref="NonBlocking"/>: opens its own independent window immediately, without queueing.</item>
/// </list>
/// </summary>
public enum NotificationMode
{
    Blocking,
    NonBlocking,
}
