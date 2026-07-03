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
