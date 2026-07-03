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
