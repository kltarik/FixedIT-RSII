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
                    && reservation.ClientUser.IsActive
                    && reservation.ProfessionalProfile.IsVerified
                    && reservation.ProfessionalProfile.User.IsActive)
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
                    && activity.User.IsActive
                    && activity.ProfessionalProfile!.IsVerified
                    && activity.ProfessionalProfile.User.IsActive)
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
                .Where(activity => activity.Type == RecommendationActivityType.CategorySearch
                    && activity.User.IsActive)
                .SelectMany(
                    activity => db.ProfessionalCategories.Where(link =>
                        link.CategoryId == activity.CategoryId
                        && link.ProfessionalProfile.IsVerified
                        && link.ProfessionalProfile.User.IsActive),
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
                .GroupBy(signal => new { signal.UserId, signal.ProfessionalId })
                .Select(group => new UserRatingData
                {
                    UserId = group.Key.UserId,
                    ProfessionalId = group.Key.ProfessionalId,
                    Label = group.Sum(signal => signal.Label)
                })
                .OrderBy(signal => signal.UserId)
                .ThenBy(signal => signal.ProfessionalId)
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
