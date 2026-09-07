using System.ComponentModel.DataAnnotations;

namespace FixedIT.API.Configuration;

public sealed class RecommendationOptions
{
    public const string SectionName = "Recommendations";

    public int Seed { get; set; } = 42;

    [Range(1, 10_000)]
    public int NumberOfIterations { get; set; } = 30;

    [Range(1, 1_000)]
    public int ApproximationRank { get; set; } = 20;

    [Range(typeof(double), "0.000001", "1")]
    public double LearningRate { get; set; } = 0.001;

    [Range(1, 10_000)]
    public int MinimumTrainingRatings { get; set; } = 5;

    [Range(1, 168)]
    public int RetrainingIntervalHours { get; set; } = 24;

    [Range(1, 1_000)]
    public int CandidatePoolSize { get; set; } = 200;
}
