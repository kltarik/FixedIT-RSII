namespace FixedIT.API.Models;

public class JobPostingImage
{
    public int Id { get; set; }
    public int JobPostingId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public JobPosting JobPosting { get; set; } = null!;
}
