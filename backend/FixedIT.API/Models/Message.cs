namespace FixedIT.API.Models;

public class Message
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public string SenderUserId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public Conversation Conversation { get; set; } = null!;
    public User SenderUser { get; set; } = null!;
}
