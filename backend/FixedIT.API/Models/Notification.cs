using FixedIT.API.Models.Enums;

namespace FixedIT.API.Models;

public class Notification
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public NotificationType Type { get; set; } = NotificationType.General;

    public User User { get; set; } = null!;
}
