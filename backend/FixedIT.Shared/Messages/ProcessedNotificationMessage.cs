namespace FixedIT.Shared.Messages;

public sealed class ProcessedNotificationMessage
{
    public Guid MessageId { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
