using FixedIT.Shared.Messages;

namespace FixedIT.NotificationService.Services;

public interface IEmailService
{
    Task SendAsync(
        BaseNotificationMessage notification,
        CancellationToken cancellationToken);
}
