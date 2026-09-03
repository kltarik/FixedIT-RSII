namespace FixedIT.API.Models;

public class ProfessionalProfile
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public int YearsOfExperience { get; set; }
    public bool IsVerified { get; set; }
    public decimal AverageRating { get; set; }

    public User User { get; set; } = null!;
    public ICollection<ProfessionalCategory> ProfessionalCategories { get; set; } = [];
    public ICollection<PortfolioItem> PortfolioItems { get; set; } = [];
    public ICollection<JobOffer> JobOffers { get; set; } = [];
    public ICollection<Reservation> Reservations { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
    public ICollection<UserRating> UserRatings { get; set; } = [];
    public ICollection<ProfessionalAvailability> Availability { get; set; } = [];
}
