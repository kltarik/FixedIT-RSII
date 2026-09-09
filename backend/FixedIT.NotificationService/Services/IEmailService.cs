using FixedIT.Shared.Messages;

namespace FixedIT.NotificationService.Services;

public interface IEmailService
{
    Task<bool> WasDeliveredAsync(
        Guid messageId,
        CancellationToken cancellationToken);

    Task SendAsync(
        BaseNotificationMessage notification,
        CancellationToken cancellationToken);
}
