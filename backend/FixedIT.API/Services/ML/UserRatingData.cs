namespace FixedIT.API.Services.ML;

public sealed class UserRatingData
{
    public string UserId { get; set; } = string.Empty;
    public string ProfessionalId { get; set; } = string.Empty;
    public float Label { get; set; }
}

public sealed class RatingPrediction
{
    public float Score { get; set; }
}
