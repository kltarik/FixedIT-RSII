namespace FixedIT.API.Models;

public class City
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = [];
    public ICollection<JobPosting> JobPostings { get; set; } = [];
}
