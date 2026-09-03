using System.Data;
using FixedIT.API.CustomExceptions;
using FixedIT.API.Data;
using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Reservations;
using FixedIT.API.Models;
using FixedIT.API.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FixedIT.API.Services;

public sealed class ReservationService(
    AppDbContext db,
    IPaginationService paginationService,
    IHttpContextAccessor httpContextAccessor,
    IReservationEventPublisher eventPublisher,
    INotificationService notificationService) : IReservationService
{
    private const string LockedProfessionalProfilesSql =
        "SELECT * FROM [ProfessionalProfiles] WITH (UPDLOCK, HOLDLOCK)";

    public async Task<ReservationResponse> CreateAsync(
        string clientUserId,
        CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        if (request.ScheduledAt.Kind != DateTimeKind.Utc)
        {
            throw new BusinessException("Termin mora biti naveden u UTC formatu sa završnim znakom Z.");
        }

        if (request.ScheduledAt <= now)
        {
            throw new BusinessException("Termin rezervacije mora biti u budućnosti.");
        }

        DateTime scheduledEnd;
        try
        {
            scheduledEnd = request.ScheduledAt.AddMinutes(request.DurationMinutes);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new BusinessException("Trajanje rezervacije daje neispravno vrijeme završetka.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var professional = await db.ProfessionalProfiles
            .FromSqlRaw(LockedProfessionalProfilesSql)
            .Include(profile => profile.User)
            .SingleOrDefaultAsync(
                profile => profile.Id == request.ProfessionalProfileId,
                cancellationToken)
            ?? throw new NotFoundException("Profil profesionalca nije pronađen.");
        if (professional.UserId == clientUserId)
        {
            throw new BusinessException("Profesionalac ne može kreirati rezervaciju sam sa sobom.");
        }

        if (professional.HourlyRate <= 0)
        {
            throw new BusinessException("Profesionalac nema ispravno definisanu satnicu.");
        }

        var supportsCategory = await db.ProfessionalCategories.AnyAsync(
            item => item.ProfessionalProfileId == professional.Id
                && item.CategoryId == request.CategoryId,
            cancellationToken);
        if (!supportsCategory)
        {
            throw new BusinessException("Profesionalac ne pruža usluge iz odabrane kategorije.");
        }

        var startTime = TimeOnly.FromDateTime(request.ScheduledAt);
        var endTime = TimeOnly.FromDateTime(scheduledEnd);
        var isWithinAvailability = request.ScheduledAt.Date == scheduledEnd.Date
            && await db.ProfessionalAvailabilities.AnyAsync(
                item => item.ProfessionalProfileId == professional.Id
                    && item.DayOfWeek == request.ScheduledAt.DayOfWeek
                    && item.StartTime <= startTime
                    && item.EndTime >= endTime,
                cancellationToken);
        if (!isWithinAvailability)
        {
            throw new BusinessException("Odabrani termin nije unutar radnog vremena profesionalca.");
        }

        var hasOverlap = await db.Reservations
            .IgnoreQueryFilters()
            .AnyAsync(
                reservation => reservation.ProfessionalProfileId == professional.Id
                    && reservation.Status != ReservationStatus.Cancelled
                    && reservation.ScheduledAt < scheduledEnd
                    && reservation.ScheduledAt.AddMinutes(reservation.DurationMinutes)
                        > request.ScheduledAt,
                cancellationToken);
        if (hasOverlap)
        {
            throw new BusinessException("Profesionalac nije dostupan u odabranom terminu.");
        }

        decimal totalPrice;
        try
        {
            totalPrice = decimal.Round(
                checked(professional.HourlyRate * request.DurationMinutes / 60m),
                2,
                MidpointRounding.AwayFromZero);
        }
        catch (OverflowException)
        {
            throw new BusinessException("Izračunata cijena rezervacije je izvan dozvoljenog raspona.");
        }

        if (totalPrice <= 0)
        {
            throw new BusinessException("Izračunata cijena rezervacije mora biti veća od nule.");
        }

        var reservation = new Reservation
        {
            ClientUserId = clientUserId,
            ProfessionalProfileId = professional.Id,
            CategoryId = request.CategoryId,
            ServiceDescription = request.ServiceDescription.Trim(),
            ScheduledAt = request.ScheduledAt,
            DurationMinutes = request.DurationMinutes,
            TotalPrice = totalPrice,
            Status = ReservationStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync(cancellationToken);

        db.AuditLogs.Add(new AuditLog
        {
            UserId = clientUserId,
            Action = "Created",
            EntityType = nameof(Reservation),
            EntityId = reservation.Id.ToString(),
            Details = "Rezervacija je kreirana sa statusom Na čekanju.",
            IpAddress = GetRemoteIpAddress(),
            CreatedAt = now
        });
        var notification = new Notification
        {
            UserId = professional.UserId,
            Title = "Nova rezervacija",
            Body = $"Zakazana je nova rezervacija za {request.ScheduledAt:dd.MM.yyyy HH:mm}.",
            IsRead = false,
            CreatedAt = now,
            Type = NotificationType.Reservation
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await notificationService.PushAsync(notification, cancellationToken);
        await eventPublisher.PublishAsync(
            new ReservationStatusChangedEvent(
                reservation.Id,
                clientUserId,
                professional.UserId,
                null,
                ReservationStatus.Pending,
                now,
                clientUserId,
                null),
            cancellationToken);
        return await GetByIdAsync(clientUserId, reservation.Id, cancellationToken);
    }

    public Task<PagedResponse<ReservationResponse>> GetMineAsync(
        string userId,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        var query = db.Reservations
            .AsNoTracking()
            .Where(reservation => reservation.ClientUserId == userId
                || reservation.ProfessionalProfile.UserId == userId);
        return GetPageAsync(query, request, cancellationToken);
    }

    public async Task<ReservationResponse> GetByIdAsync(
        string userId,
        int reservationId,
        CancellationToken cancellationToken)
    {
        return await db.Reservations
            .AsNoTracking()
            .Where(reservation => reservation.Id == reservationId
                && (reservation.ClientUserId == userId
                    || reservation.ProfessionalProfile.UserId == userId))
            .Select(ReservationProjection.Response)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Rezervacija nije pronađena.");
    }

    public Task<PagedResponse<ReservationResponse>> GetAdminPageAsync(
        AdminReservationFilterRequest filters,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        var query = db.Reservations.IgnoreQueryFilters().AsNoTracking();
        if (filters.Status.HasValue)
        {
            query = query.Where(reservation => reservation.Status == filters.Status.Value);
        }

        return GetPageAsync(query, request, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AvailableSlotResponse>> GetAvailableSlotsAsync(
        int professionalProfileId,
        AvailableSlotsRequest request,
        CancellationToken cancellationToken)
    {
        var dateStart = DateTime.SpecifyKind(
            request.Date.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);
        if (dateStart.Date < DateTime.UtcNow.Date
            || dateStart.Date > DateTime.UtcNow.Date.AddYears(1))
        {
            throw new BusinessException("Datum termina mora biti unutar narednih godinu dana.");
        }

        var supportsCategory = await db.ProfessionalCategories.AnyAsync(
            item => item.ProfessionalProfileId == professionalProfileId
                && item.CategoryId == request.CategoryId,
            cancellationToken);
        if (!supportsCategory)
        {
            throw new NotFoundException("Profesionalac nije pronađen za odabranu kategoriju.");
        }

        var periods = await db.ProfessionalAvailabilities
            .AsNoTracking()
            .Where(item => item.ProfessionalProfileId == professionalProfileId
                && item.DayOfWeek == dateStart.DayOfWeek)
            .OrderBy(item => item.StartTime)
            .Select(item => new { item.StartTime, item.EndTime })
            .ToArrayAsync(cancellationToken);
        var dateEnd = dateStart.AddDays(1);
        var reservations = await db.Reservations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(item => item.ProfessionalProfileId == professionalProfileId
                && item.Status != ReservationStatus.Cancelled
                && item.ScheduledAt >= dateStart
                && item.ScheduledAt < dateEnd)
            .Select(item => new
            {
                Start = item.ScheduledAt,
                End = item.ScheduledAt.AddMinutes(item.DurationMinutes)
            })
            .ToArrayAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var slots = new List<AvailableSlotResponse>();
        foreach (var period in periods)
        {
            var cursor = DateTime.SpecifyKind(
                request.Date.ToDateTime(period.StartTime),
                DateTimeKind.Utc);
            var periodEnd = DateTime.SpecifyKind(
                request.Date.ToDateTime(period.EndTime),
                DateTimeKind.Utc);
            while (cursor.AddMinutes(request.DurationMinutes) <= periodEnd)
            {
                var end = cursor.AddMinutes(request.DurationMinutes);
                if (cursor > now
                    && reservations.All(item => item.Start >= end || item.End <= cursor))
                {
                    slots.Add(new AvailableSlotResponse(cursor, end));
                }

                cursor = cursor.AddMinutes(30);
            }
        }

        return slots;
    }

    public async Task<IReadOnlyCollection<ProfessionalAvailabilityResponse>> GetMyAvailabilityAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        return await db.ProfessionalAvailabilities
            .AsNoTracking()
            .Where(item => item.ProfessionalProfile.UserId == userId)
            .OrderBy(item => item.DayOfWeek)
            .ThenBy(item => item.StartTime)
            .Select(item => new ProfessionalAvailabilityResponse(
                item.Id,
                item.DayOfWeek,
                item.StartTime,
                item.EndTime))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProfessionalAvailabilityResponse>> SaveMyAvailabilityAsync(
        string userId,
        SaveProfessionalAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var professionalId = await db.ProfessionalProfiles
            .Where(profile => profile.UserId == userId)
            .Select(profile => (int?)profile.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Profil profesionalca nije pronađen.");
        var periods = request.Periods
            .OrderBy(item => item.DayOfWeek)
            .ThenBy(item => item.StartTime)
            .ToArray();
        foreach (var day in periods.GroupBy(item => item.DayOfWeek))
        {
            ProfessionalAvailabilityInput? previous = null;
            foreach (var period in day)
            {
                if (previous is not null && previous.EndTime > period.StartTime)
                {
                    throw new BusinessException("Periodi dostupnosti se ne smiju preklapati.");
                }

                previous = period;
            }
        }

        var existing = await db.ProfessionalAvailabilities
            .Where(item => item.ProfessionalProfileId == professionalId)
            .ToArrayAsync(cancellationToken);
        db.ProfessionalAvailabilities.RemoveRange(existing);
        db.ProfessionalAvailabilities.AddRange(periods.Select(item => new ProfessionalAvailability
        {
            ProfessionalProfileId = professionalId,
            DayOfWeek = item.DayOfWeek,
            StartTime = item.StartTime,
            EndTime = item.EndTime
        }));
        await db.SaveChangesAsync(cancellationToken);
        return await GetMyAvailabilityAsync(userId, cancellationToken);
    }

    private async Task<PagedResponse<ReservationResponse>> GetPageAsync(
        IQueryable<Reservation> query,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        var page = paginationService.Normalize(request);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(reservation => reservation.ScheduledAt)
            .ThenByDescending(reservation => reservation.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(ReservationProjection.Response)
            .ToListAsync(cancellationToken);

        return new PagedResponse<ReservationResponse>(
            items,
            total,
            page.Page,
            page.PageSize);
    }

    private string GetRemoteIpAddress()
    {
        return httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
            ?? "nepoznato";
    }
}
