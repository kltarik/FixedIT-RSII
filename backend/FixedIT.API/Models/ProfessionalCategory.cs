namespace FixedIT.API.Models;

public class ProfessionalCategory
{
    public int ProfessionalProfileId { get; set; }
    public int CategoryId { get; set; }

    public ProfessionalProfile ProfessionalProfile { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
