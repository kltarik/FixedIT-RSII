namespace FixedIT.API.Models;

public class Conversation
{
    public int Id { get; set; }
    public int? ReservationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Reservation? Reservation { get; set; }
    public ICollection<ConversationParticipant> Participants { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
}
