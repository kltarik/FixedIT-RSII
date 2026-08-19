namespace FixedIT.API.Models;

public class UserRating
{
    public int Id { get; set; }
    public int? ReviewId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int ProfessionalProfileId { get; set; }
    public int Rating { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ProfessionalProfile ProfessionalProfile { get; set; } = null!;
    public Review? Review { get; set; }
}
