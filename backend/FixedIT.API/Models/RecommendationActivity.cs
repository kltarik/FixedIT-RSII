using FixedIT.API.Models.Enums;

namespace FixedIT.API.Models;

public class RecommendationActivity
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public RecommendationActivityType Type { get; set; }
    public int? ProfessionalProfileId { get; set; }
    public int? CategoryId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ProfessionalProfile? ProfessionalProfile { get; set; }
    public Category? Category { get; set; }
}
