using FixedIT.API.Configuration;
using FixedIT.API.Data;
using FixedIT.API.Services.ML;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FixedIT.API.BackgroundServices;

public sealed class ModelRetrainingService(
    IServiceScopeFactory serviceScopeFactory,
    RecommendationService recommendationService,
    IOptions<RecommendationOptions> options,
    ILogger<ModelRetrainingService> logger) : BackgroundService
{
    private readonly TimeSpan _retrainingInterval = TimeSpan.FromHours(
        options.Value.RetrainingIntervalHours);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RetrainSafelyAsync(stoppingToken);

        using var timer = new PeriodicTimer(_retrainingInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RetrainSafelyAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Recommendation model retraining stopped.");
        }
    }

    private async Task RetrainSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ratings = await db.UserRatings
                .AsNoTracking()
                .OrderBy(rating => rating.Id)
                .Select(rating => new UserRatingData
                {
                    UserId = rating.UserId,
                    ProfessionalId = rating.ProfessionalProfileId.ToString(),
                    Label = rating.Rating
                })
                .ToListAsync(cancellationToken);

            recommendationService.TrainModel(ratings);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Recommendation model retraining failed.");
        }
    }
}
