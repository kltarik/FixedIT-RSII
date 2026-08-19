using FixedIT.API.DTOs.Notifications;

namespace FixedIT.API.Hubs;

public interface INotificationsClient
{
    Task ReceiveNotification(NotificationResponse notification);
}
