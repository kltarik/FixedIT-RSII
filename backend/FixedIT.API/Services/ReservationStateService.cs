using System.Data;
using FixedIT.API.Constants;
using FixedIT.API.CustomExceptions;
using FixedIT.API.Data;
using FixedIT.API.DTOs.Reservations;
using FixedIT.API.Models;
using FixedIT.API.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FixedIT.API.Services;

public sealed class ReservationStateService(
    AppDbContext db,
    IPaymentRefundService paymentRefundService,
    IReservationEventPublisher eventPublisher,
    INotificationService notificationService,
    IHttpContextAccessor httpContextAccessor) : IReservationStateService
{
    private const string LockedReservationsSql =
        "SELECT * FROM [Reservations] WITH (UPDLOCK, HOLDLOCK)";

    public async Task<ReservationResponse> TransitionAsync(
        int reservationId,
        ReservationStatus newStatus,
        string userId,
        string? cancellationReason,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var reservation = await db.Reservations
            .FromSqlRaw(LockedReservationsSql)
            .IgnoreQueryFilters()
            .Include(item => item.ProfessionalProfile)
            .ThenInclude(profile => profile.User)
            .Include(item => item.Payment)
            .SingleOrDefaultAsync(item => item.Id == reservationId, cancellationToken)
            ?? throw new NotFoundException("Rezervacija nije pronađena.");
        var isAdmin = await IsAdminAsync(userId, cancellationToken);
        var isClient = reservation.ClientUserId == userId;
        var isProfessional = reservation.ProfessionalProfile.UserId == userId;
        if (!isAdmin && !isClient && !isProfessional)
        {
            throw new NotFoundException("Rezervacija nije pronađena.");
        }

        ValidateTransition(
            reservation.Status,
            newStatus,
            isClient,
            isProfessional,
            isAdmin);
        var normalizedReason = NormalizeCancellationReason(newStatus, cancellationReason);
        if (newStatus == ReservationStatus.Cancelled && reservation.Payment is not null)
        {
            await paymentRefundService.RefundAsync(reservation.Payment, cancellationToken);
        }

        var previousStatus = reservation.Status;
        var now = DateTime.UtcNow;
        reservation.Status = newStatus;
        reservation.UpdatedAt = now;
        reservation.CancellationReason = normalizedReason;

        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "StatusChanged",
            EntityType = nameof(Reservation),
            EntityId = reservation.Id.ToString(),
            Details = BuildAuditDetails(previousStatus, newStatus, normalizedReason),
            IpAddress = GetRemoteIpAddress(),
            CreatedAt = now
        });
        var notifications = AddParticipantNotifications(
            reservation,
            userId,
            isAdmin,
            previousStatus,
            newStatus,
            normalizedReason,
            now);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            await notificationService.PushAsync(notification, cancellationToken);
        }

        await eventPublisher.PublishAsync(
            new ReservationStatusChangedEvent(
                reservation.Id,
                reservation.ClientUserId,
                reservation.ProfessionalProfile.UserId,
                previousStatus,
                newStatus,
                now,
                userId,
                normalizedReason),
            cancellationToken);
        return await GetResponseAsync(reservation.Id, cancellationToken);
    }

    private static void ValidateTransition(
        ReservationStatus currentStatus,
        ReservationStatus newStatus,
        bool isClient,
        bool isProfessional,
        bool isAdmin)
    {
        var isAllowed = (currentStatus, newStatus) switch
        {
            (ReservationStatus.Pending, ReservationStatus.Accepted) => isProfessional || isAdmin,
            (ReservationStatus.Accepted, ReservationStatus.InProgress) => isProfessional || isAdmin,
            (ReservationStatus.InProgress, ReservationStatus.Completed) => isProfessional || isAdmin,
            (ReservationStatus.Pending, ReservationStatus.Cancelled) =>
                isClient || isProfessional || isAdmin,
            (ReservationStatus.Accepted, ReservationStatus.Cancelled) => isClient || isAdmin,
            (ReservationStatus.InProgress, ReservationStatus.Cancelled) => isAdmin,
            _ => false
        };
        if (!isAllowed)
        {
            throw new BusinessException(
                $"Prijelaz iz statusa {LocalizeStatus(currentStatus)} u status {LocalizeStatus(newStatus)} nije dozvoljen ovom korisniku.");
        }
    }

    private static string? NormalizeCancellationReason(
        ReservationStatus newStatus,
        string? cancellationReason)
    {
        if (newStatus != ReservationStatus.Cancelled)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(cancellationReason))
        {
            throw new BusinessException("Razlog otkazivanja je obavezan.");
        }

        return cancellationReason.Trim();
    }

    private static string LocalizeStatus(ReservationStatus status) => status switch
    {
        ReservationStatus.Pending => "na čekanju",
        ReservationStatus.Accepted => "prihvaćena",
        ReservationStatus.InProgress => "u toku",
        ReservationStatus.Completed => "završena",
        ReservationStatus.Cancelled => "otkazana",
        _ => "nepoznat"
    };

    private async Task<bool> IsAdminAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        return await db.UserRoles
            .Where(link => link.UserId == userId)
            .Join(
                db.Roles,
                link => link.RoleId,
                role => role.Id,
                (link, role) => role.Name)
            .AnyAsync(roleName => roleName == RoleNames.Admin, cancellationToken);
    }

    private IReadOnlyCollection<Notification> AddParticipantNotifications(
        Reservation reservation,
        string actorUserId,
        bool isAdmin,
        ReservationStatus previousStatus,
        ReservationStatus newStatus,
        string? reason,
        DateTime createdAt)
    {
        var recipients = new HashSet<string>(StringComparer.Ordinal);
        if (isAdmin || reservation.ClientUserId != actorUserId)
        {
            recipients.Add(reservation.ClientUserId);
        }

        if (isAdmin || reservation.ProfessionalProfile.UserId != actorUserId)
        {
            recipients.Add(reservation.ProfessionalProfile.UserId);
        }

        var body = $"Status rezervacije je promijenjen sa {StatusName(previousStatus)} na {StatusName(newStatus)}.";
        if (!string.IsNullOrWhiteSpace(reason))
        {
            body = $"{body} Razlog: {reason}";
        }

        var notifications = recipients.Select(recipient => new Notification
        {
            UserId = recipient,
            Title = "Status rezervacije je promijenjen",
            Body = body,
            IsRead = false,
            CreatedAt = createdAt,
            Type = NotificationType.Reservation
        }).ToArray();
        db.Notifications.AddRange(notifications);
        return notifications;
    }

    private async Task<ReservationResponse> GetResponseAsync(
        int reservationId,
        CancellationToken cancellationToken)
    {
        return await db.Reservations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(reservation => reservation.Id == reservationId)
            .Select(ReservationProjection.Response)
            .SingleAsync(cancellationToken);
    }

    private static string BuildAuditDetails(
        ReservationStatus previousStatus,
        ReservationStatus newStatus,
        string? reason)
    {
        var details = $"Status: {StatusName(previousStatus)} -> {StatusName(newStatus)}.";
        return string.IsNullOrWhiteSpace(reason)
            ? details
            : $"{details} Razlog: {reason}";
    }

    private string GetRemoteIpAddress()
    {
        return httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
            ?? "nepoznato";
    }

    private static string StatusName(ReservationStatus status) => status switch
    {
        ReservationStatus.Pending    => "Na čekanju",
        ReservationStatus.Accepted   => "Prihvaćena",
        ReservationStatus.InProgress => "U toku",
        ReservationStatus.Completed  => "Završena",
        ReservationStatus.Cancelled  => "Otkazana",
        _                            => status.ToString()
    };
}
