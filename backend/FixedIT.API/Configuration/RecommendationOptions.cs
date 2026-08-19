using System.ComponentModel.DataAnnotations;

namespace FixedIT.API.Configuration;

public sealed class RecommendationOptions
{
    public const string SectionName = "Recommendations";

    public int Seed { get; set; } = 42;

    [Range(1, 10_000)]
    public int NumberOfIterations { get; set; } = 20;

    [Range(1, 1_000)]
    public int ApproximationRank { get; set; } = 100;

    [Range(1, 10_000)]
    public int MinimumTrainingRatings { get; set; } = 5;

    [Range(1, 168)]
    public int RetrainingIntervalHours { get; set; } = 24;

    [Range(1, 1_000)]
    public int CandidatePoolSize { get; set; } = 200;
}
