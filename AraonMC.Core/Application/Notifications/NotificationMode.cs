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
