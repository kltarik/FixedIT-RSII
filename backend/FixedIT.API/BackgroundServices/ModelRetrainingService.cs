using FixedIT.API.Configuration;
using FixedIT.API.Data;
using FixedIT.API.Services.ML;
using FixedIT.API.Models.Enums;
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
            var reservationSignals = await db.Reservations
                .AsNoTracking()
                .Where(reservation => reservation.Status != ReservationStatus.Cancelled
                    && reservation.ProfessionalProfile.IsVerified)
                .OrderBy(reservation => reservation.Id)
                .Select(reservation => new UserRatingData
                {
                    UserId = reservation.ClientUserId,
                    ProfessionalId = reservation.ProfessionalProfileId.ToString(),
                    Label = 5
                })
                .ToListAsync(cancellationToken);
            var profileViewSignals = await db.RecommendationActivities
                .AsNoTracking()
                .Where(activity => activity.Type == RecommendationActivityType.ProfileView
                    && activity.ProfessionalProfile!.IsVerified)
                .OrderBy(activity => activity.Id)
                .Select(activity => new UserRatingData
                {
                    UserId = activity.UserId,
                    ProfessionalId = activity.ProfessionalProfileId!.Value.ToString(),
                    Label = 2
                })
                .ToListAsync(cancellationToken);
            var categorySearchSignals = await db.RecommendationActivities
                .AsNoTracking()
                .Where(activity => activity.Type == RecommendationActivityType.CategorySearch)
                .SelectMany(
                    activity => db.ProfessionalCategories.Where(link =>
                        link.CategoryId == activity.CategoryId
                        && link.ProfessionalProfile.IsVerified),
                    (activity, link) => new UserRatingData
                    {
                        UserId = activity.UserId,
                        ProfessionalId = link.ProfessionalProfileId.ToString(),
                        Label = 1
                    })
                .ToListAsync(cancellationToken);

            var signals = reservationSignals
                .Concat(profileViewSignals)
                .Concat(categorySearchSignals)
                .ToArray();

            recommendationService.TrainModel(signals);
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
