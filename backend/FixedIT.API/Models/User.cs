using Microsoft.AspNetCore.Identity;

namespace FixedIT.API.Models;

public class User : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public int CityId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public City City { get; set; } = null!;
    public ProfessionalProfile? ProfessionalProfile { get; set; }
    public ICollection<JobPosting> JobPostings { get; set; } = [];
    public ICollection<Reservation> ClientReservations { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
    public ICollection<ConversationParticipant> ConversationParticipations { get; set; } = [];
    public ICollection<Message> SentMessages { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public ICollection<AuditLog> AuditLogs { get; set; } = [];
    public ICollection<UserRating> Ratings { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
