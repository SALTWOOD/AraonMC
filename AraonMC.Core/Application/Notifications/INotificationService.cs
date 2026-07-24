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

using System.Threading;
using System.Threading.Tasks;

namespace AraonMC.Core.Application.Notifications;

/// <summary>
/// Displays popup notifications (application port). Implemented by the presentation layer.
/// </summary>
/// <remarks>
/// <para>
/// <b>Blocking</b> notifications (<see cref="NotificationMode.Blocking"/>) are shown one at a time.
/// If several are requested, they queue and appear sequentially as the user dismisses each one.
/// The <see cref="ShowAsync"/> task for a blocking notification completes only once that notification
/// has been dismissed.
/// </para>
/// <para>
/// <b>Non-blocking</b> notifications (<see cref="NotificationMode.NonBlocking"/>) each open their own
/// independent window immediately — they never queue and never wait for one another.
/// The <see cref="ShowAsync"/> task completes right away.
/// </para>
/// <para>
/// Must be called on the UI thread (typical for command/view-model code).
/// </para>
/// </remarks>
public interface INotificationService
{
    /// <summary>
    /// Shows <paramref name="request"/>. See the interface remarks for blocking vs. non-blocking semantics.
    /// </summary>
    Task ShowAsync(NotificationRequest request, CancellationToken ct = default);
}
