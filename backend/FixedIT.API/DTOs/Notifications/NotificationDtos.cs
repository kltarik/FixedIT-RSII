using FixedIT.API.Models.Enums;

namespace FixedIT.API.DTOs.Notifications;

public sealed record NotificationResponse(
    int Id,
    string Title,
    string Body,
    bool IsRead,
    DateTime CreatedAt,
    NotificationType Type);
