using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Notifications;
using FixedIT.API.Models;
using FixedIT.API.Models.Enums;

namespace FixedIT.API.Services;

public sealed record CreateNotificationCommand(
    string UserId,
    string Title,
    string Body,
    NotificationType Type,
    DateTime CreatedAt);

public interface INotificationService
{
    Task<PagedResponse<NotificationResponse>> GetUnreadAsync(
        string userId,
        PagedRequest request,
        CancellationToken cancellationToken);

    Task<NotificationResponse> MarkReadAsync(
        string userId,
        int notificationId,
        CancellationToken cancellationToken);

    Task MarkAllReadAsync(string userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Notification>> CreateAndPushAsync(
        IReadOnlyCollection<CreateNotificationCommand> commands,
        CancellationToken cancellationToken);

    Task PushAsync(Notification notification, CancellationToken cancellationToken);
}
