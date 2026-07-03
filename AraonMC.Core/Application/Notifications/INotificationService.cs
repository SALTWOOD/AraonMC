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
