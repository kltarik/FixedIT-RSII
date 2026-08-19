using FixedIT.API.Models.Enums;

namespace FixedIT.API.Models;

public class JobOffer
{
    public int Id { get; set; }
    public int JobPostingId { get; set; }
    public int ProfessionalProfileId { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal ProposedPrice { get; set; }
    public JobOfferStatus Status { get; set; } = JobOfferStatus.Pending;

    public JobPosting JobPosting { get; set; } = null!;
    public ProfessionalProfile ProfessionalProfile { get; set; } = null!;
}
