namespace FixedIT.API.Models;

public class ProfessionalAvailability
{
    public int Id { get; set; }
    public int ProfessionalProfileId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public ProfessionalProfile ProfessionalProfile { get; set; } = null!;
}
