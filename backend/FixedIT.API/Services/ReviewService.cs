using System.Data;
using System.Linq.Expressions;
using FixedIT.API.CustomExceptions;
using FixedIT.API.Data;
using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Reviews;
using FixedIT.API.Models;
using FixedIT.API.Models.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FixedIT.API.Services;

public sealed class ReviewService(
    AppDbContext db,
    IPaginationService paginationService,
    IHttpContextAccessor httpContextAccessor) : IReviewService
{
    private const string LockedProfessionalProfilesSql =
        "SELECT * FROM [ProfessionalProfiles] WITH (UPDLOCK, HOLDLOCK)";

    private static readonly Expression<Func<Review, ReviewResponse>> ReviewProjection =
        review => new ReviewResponse(
            review.Id,
            review.ReservationId,
            review.ProfessionalProfileId,
            review.ClientUser.FirstName,
            review.ClientUser.LastName,
            review.ClientUser.ProfilePictureUrl,
            review.Rating,
            review.Comment,
            review.CreatedAt);

    public async Task<ReviewResponse> CreateAsync(
        string clientUserId,
        CreateReviewRequest request,
        CancellationToken cancellationToken)
    {
        var reservationInfo = await db.Reservations
            .Where(reservation => reservation.Id == request.ReservationId
                && reservation.ClientUserId == clientUserId)
            .Select(reservation => new
            {
                reservation.ProfessionalProfileId,
                reservation.Status
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Rezervacija nije pronađena.");
        if (reservationInfo.Status != ReservationStatus.Completed)
        {
            throw new BusinessException("Recenziju je moguće ostaviti samo za završenu rezervaciju.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var professional = await GetLockedProfessionalAsync(
            reservationInfo.ProfessionalProfileId,
            cancellationToken);
        var reservation = await db.Reservations
            .SingleOrDefaultAsync(
                item => item.Id == request.ReservationId
                    && item.ClientUserId == clientUserId,
                cancellationToken)
            ?? throw new NotFoundException("Rezervacija nije pronađena.");
        if (reservation.Status != ReservationStatus.Completed)
        {
            throw new BusinessException("Recenziju je moguće ostaviti samo za završenu rezervaciju.");
        }

        if (await db.Reviews.IgnoreQueryFilters().AnyAsync(
                review => review.ReservationId == reservation.Id,
                cancellationToken))
        {
            throw new BusinessException("Rezervaciju je moguće recenzirati samo jednom.");
        }

        var now = DateTime.UtcNow;
        var review = new Review
        {
            ReservationId = reservation.Id,
            ClientUserId = clientUserId,
            ProfessionalProfileId = professional.Id,
            Rating = request.Rating,
            Comment = request.Comment.Trim(),
            CreatedAt = now
        };
        var userRating = new UserRating
        {
            UserId = clientUserId,
            ProfessionalProfileId = professional.Id,
            Rating = request.Rating,
            Timestamp = now,
            Review = review
        };
        db.Reviews.Add(review);
        db.UserRatings.Add(userRating);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new BusinessException("Rezervaciju je moguće recenzirati samo jednom.");
        }

        professional.AverageRating = await CalculateAverageAsync(
            professional.Id,
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetReviewAsync(review.Id, cancellationToken);
    }

    public async Task<PagedResponse<ReviewResponse>> GetForProfessionalAsync(
        int professionalProfileId,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        if (!await db.ProfessionalProfiles.AnyAsync(
                profile => profile.Id == professionalProfileId && profile.IsVerified,
                cancellationToken))
        {
            throw new NotFoundException("Profil profesionalca nije pronađen.");
        }

        var page = paginationService.Normalize(request);
        var query = db.Reviews
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(review => review.ProfessionalProfileId == professionalProfileId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(review => review.CreatedAt)
            .ThenByDescending(review => review.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(ReviewProjection)
            .ToListAsync(cancellationToken);

        return new PagedResponse<ReviewResponse>(
            items,
            total,
            page.Page,
            page.PageSize);
    }

    public async Task<PagedResponse<AdminReviewResponse>> GetAdminPageAsync(
        AdminReviewFilterRequest filters,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        var page = paginationService.Normalize(request);
        var query = db.Reviews.IgnoreQueryFilters().AsNoTracking();
        if (filters.Rating.HasValue)
        {
            query = query.Where(review => review.Rating == filters.Rating.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var search = filters.Search.Trim();
            query = query.Where(review =>
                review.Comment.Contains(search)
                || review.ClientUser.FirstName.Contains(search)
                || review.ClientUser.LastName.Contains(search)
                || review.ProfessionalProfile.User.FirstName.Contains(search)
                || review.ProfessionalProfile.User.LastName.Contains(search));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(review => review.CreatedAt)
            .ThenByDescending(review => review.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(review => new AdminReviewResponse(
                review.Id,
                review.ReservationId,
                review.ProfessionalProfileId,
                review.ClientUser.FirstName + " " + review.ClientUser.LastName,
                review.ProfessionalProfile.User.FirstName + " " + review.ProfessionalProfile.User.LastName,
                review.Rating,
                review.Comment,
                review.CreatedAt))
            .ToArrayAsync(cancellationToken);
        return new PagedResponse<AdminReviewResponse>(items, total, page.Page, page.PageSize);
    }

    public async Task DeleteAsync(
        string adminUserId,
        int reviewId,
        CancellationToken cancellationToken)
    {
        var professionalProfileId = await db.Reviews
            .IgnoreQueryFilters()
            .Where(review => review.Id == reviewId)
            .Select(review => (int?)review.ProfessionalProfileId)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Recenzija nije pronađena.");

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var professional = await GetLockedProfessionalAsync(
            professionalProfileId,
            cancellationToken,
            ignoreQueryFilters: true);
        var review = await db.Reviews
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.Id == reviewId, cancellationToken)
            ?? throw new NotFoundException("Recenzija nije pronađena.");
        db.Reviews.Remove(review);
        await db.SaveChangesAsync(cancellationToken);

        professional.AverageRating = await CalculateAverageAsync(
            professional.Id,
            cancellationToken);
        db.AuditLogs.Add(new AuditLog
        {
            UserId = adminUserId,
            Action = "Deleted",
            EntityType = nameof(Review),
            EntityId = review.Id.ToString(),
            Details = $"Recenzija za rezervaciju {review.ReservationId} uklonjena je moderacijom.",
            IpAddress = GetRemoteIpAddress(),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<ProfessionalProfile> GetLockedProfessionalAsync(
        int professionalProfileId,
        CancellationToken cancellationToken,
        bool ignoreQueryFilters = false)
    {
        var query = db.ProfessionalProfiles.FromSqlRaw(LockedProfessionalProfilesSql);
        if (ignoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        return await query.SingleOrDefaultAsync(
            profile => profile.Id == professionalProfileId,
            cancellationToken)
            ?? throw new NotFoundException("Profil profesionalca nije pronađen.");
    }

    private async Task<decimal> CalculateAverageAsync(
        int professionalProfileId,
        CancellationToken cancellationToken)
    {
        var average = await db.Reviews
            .IgnoreQueryFilters()
            .Where(review => review.ProfessionalProfileId == professionalProfileId)
            .Select(review => (decimal?)review.Rating)
            .AverageAsync(cancellationToken);
        return decimal.Round(
            average ?? 0m,
            2,
            MidpointRounding.AwayFromZero);
    }

    private async Task<ReviewResponse> GetReviewAsync(
        int reviewId,
        CancellationToken cancellationToken)
    {
        return await db.Reviews
            .AsNoTracking()
            .Where(review => review.Id == reviewId)
            .Select(ReviewProjection)
            .SingleAsync(cancellationToken);
    }

    private string GetRemoteIpAddress()
    {
        return httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException { Number: 2601 or 2627 };
    }
}
