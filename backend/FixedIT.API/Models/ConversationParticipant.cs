namespace FixedIT.API.Models;

public class ConversationParticipant
{
    public int ConversationId { get; set; }
    public string UserId { get; set; } = string.Empty;

    public Conversation Conversation { get; set; } = null!;
    public User User { get; set; } = null!;
}
