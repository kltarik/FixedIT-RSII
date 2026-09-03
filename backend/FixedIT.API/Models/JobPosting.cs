using FixedIT.API.Models.Enums;

namespace FixedIT.API.Models;

public class JobPosting
{
    public int Id { get; set; }
    public string ClientUserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int CityId { get; set; }
    public decimal Budget { get; set; }
    public JobPostingStatus Status { get; set; } = JobPostingStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User ClientUser { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public City City { get; set; } = null!;
    public ICollection<JobOffer> Offers { get; set; } = [];
    public ICollection<JobPostingImage> Images { get; set; } = [];
}
