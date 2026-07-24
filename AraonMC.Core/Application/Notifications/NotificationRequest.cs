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
/// Describes a single notification to display via <c>INotificationService</c>.
/// Construct directly, or use the <see cref="Dialog"/> / <see cref="Toast"/> helpers.
/// </summary>
public sealed record NotificationRequest
{
    /// <summary>Short headline shown in bold.</summary>
    public required string Title { get; init; }

    /// <summary>Optional body text. Hidden when empty.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Icon glyph + accent color.</summary>
    public NotificationLevel Level { get; init; } = NotificationLevel.Info;

    /// <summary>Blocking queue vs. independent non-blocking window.</summary>
    public NotificationMode Mode { get; init; } = NotificationMode.Blocking;

    /// <summary>
    /// Non-blocking only: time until the window auto-closes itself.
    /// <see langword="null"/> keeps the window open until the user dismisses it.
    /// Ignored for <see cref="NotificationMode.Blocking"/>.
    /// </summary>
    public TimeSpan? AutoDismiss { get; init; }

    /// <summary>
    /// A blocking modal notification. Queues behind other blocking notifications and is
    /// shown one at a time. Use <paramref name="level"/> to convey severity.
    /// </summary>
    public static NotificationRequest Dialog(
        string title,
        string message = "",
        NotificationLevel level = NotificationLevel.Info) =>
        new()
        {
            Title = title,
            Message = message,
            Level = level,
            Mode = NotificationMode.Blocking,
        };

    /// <summary>
    /// A non-blocking notification that opens its own independent window immediately.
    /// Auto-closes after <paramref name="autoDismiss"/> (default 5s); pass <see langword="null"/>
    /// to keep it open until the user dismisses it.
    /// </summary>
    public static NotificationRequest Toast(
        string title,
        string message = "",
        NotificationLevel level = NotificationLevel.Info,
        TimeSpan? autoDismiss = null) =>
        new()
        {
            Title = title,
            Message = message,
            Level = level,
            Mode = NotificationMode.NonBlocking,
            AutoDismiss = autoDismiss ?? TimeSpan.FromSeconds(5),
        };
}
