namespace FixedIT.API.Models;

public class Review
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public string ClientUserId { get; set; } = string.Empty;
    public int ProfessionalProfileId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Reservation Reservation { get; set; } = null!;
    public User ClientUser { get; set; } = null!;
    public ProfessionalProfile ProfessionalProfile { get; set; } = null!;
    public UserRating? UserRating { get; set; }
}
