using FixedIT.API.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.ML;
using Microsoft.ML.Trainers;

namespace FixedIT.API.Services.ML;

public sealed class RecommendationService(
    IOptions<RecommendationOptions> options,
    ILogger<RecommendationService> logger) : IDisposable
{
    private const string UserIdEncoded = "UserIdEncoded";
    private const string ProfessionalIdEncoded = "ProfessionalIdEncoded";

    private readonly object _modelLock = new();
    private readonly RecommendationOptions _options = options.Value;
    private PredictionEngine<UserRatingData, RatingPrediction>? _engine;
    private HashSet<string> _knownUserIds = [];
    private HashSet<string> _knownProfessionalIds = [];
    private bool _disposed;

    public DateTime? LastTrainedAtUtc { get; private set; }
    public int TrainingSignalCount { get; private set; }

    public bool TrainModel(IReadOnlyCollection<UserRatingData> ratings)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (ratings.Count < _options.MinimumTrainingRatings)
        {
            logger.LogWarning(
                "Recommendation model training skipped because only {RatingCount} of {MinimumRatingCount} required ratings are available.",
                ratings.Count,
                _options.MinimumTrainingRatings);
            return false;
        }

        var mlContext = new MLContext(seed: _options.Seed);
        var data = mlContext.Data.LoadFromEnumerable(ratings);
        var pipeline = mlContext.Transforms.Conversion
            .MapValueToKey(UserIdEncoded, nameof(UserRatingData.UserId))
            .Append(mlContext.Transforms.Conversion.MapValueToKey(
                ProfessionalIdEncoded,
                nameof(UserRatingData.ProfessionalId)))
            .Append(mlContext.Recommendation().Trainers.MatrixFactorization(
                new MatrixFactorizationTrainer.Options
                {
                    LabelColumnName = nameof(UserRatingData.Label),
                    MatrixColumnIndexColumnName = UserIdEncoded,
                    MatrixRowIndexColumnName = ProfessionalIdEncoded,
                    NumberOfIterations = _options.NumberOfIterations,
                    ApproximationRank = _options.ApproximationRank
                }));
        var model = pipeline.Fit(data);
        var newEngine = mlContext.Model.CreatePredictionEngine<UserRatingData, RatingPrediction>(
            model);
        var knownUserIds = ratings
            .Select(rating => rating.UserId)
            .ToHashSet(StringComparer.Ordinal);
        var knownProfessionalIds = ratings
            .Select(rating => rating.ProfessionalId)
            .ToHashSet(StringComparer.Ordinal);

        lock (_modelLock)
        {
            _engine?.Dispose();
            _engine = newEngine;
            _knownUserIds = knownUserIds;
            _knownProfessionalIds = knownProfessionalIds;
            TrainingSignalCount = ratings.Count;
            LastTrainedAtUtc = DateTime.UtcNow;
        }

        logger.LogInformation(
            "Recommendation model trained from {RatingCount} weighted activity signals at {TrainedAtUtc}.",
            ratings.Count,
            LastTrainedAtUtc);
        return true;
    }

    public IReadOnlyDictionary<int, float> PredictRatings(
        string userId,
        IEnumerable<int> professionalIds)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_modelLock)
        {
            if (_engine is null || !_knownUserIds.Contains(userId))
            {
                return new Dictionary<int, float>();
            }

            var predictions = new Dictionary<int, float>();
            foreach (var professionalId in professionalIds.Distinct())
            {
                var professionalIdValue = professionalId.ToString();
                if (!_knownProfessionalIds.Contains(professionalIdValue))
                {
                    continue;
                }

                var score = _engine.Predict(new UserRatingData
                {
                    UserId = userId,
                    ProfessionalId = professionalIdValue
                }).Score;
                if (float.IsFinite(score))
                {
                    predictions[professionalId] = Math.Clamp(score, 1f, 5f);
                }
            }

            return predictions;
        }
    }

    public void Dispose()
    {
        lock (_modelLock)
        {
            if (_disposed)
            {
                return;
            }

            _engine?.Dispose();
            _engine = null;
            _disposed = true;
        }
    }
}
