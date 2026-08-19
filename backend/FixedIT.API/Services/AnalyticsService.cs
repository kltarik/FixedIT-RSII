using FixedIT.API.Configuration;
using FixedIT.API.Data;
using FixedIT.API.DTOs.Reports;
using FixedIT.API.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FixedIT.API.Services;

public sealed class AnalyticsService(
    AppDbContext db,
    IOptions<PayPalOptions> payPalOptions) : IAnalyticsService
{
    private readonly string _currency = payPalOptions.Value.Currency;

    public async Task<AdminStatsResponse> GetAdminStatsAsync(
        CancellationToken cancellationToken)
    {
        var totalUsers = await db.Users.IgnoreQueryFilters().CountAsync(cancellationToken);
        var activeUsers = await db.Users.IgnoreQueryFilters()
            .CountAsync(user => user.IsActive, cancellationToken);
        var totalProfessionals = await db.ProfessionalProfiles.IgnoreQueryFilters()
            .CountAsync(cancellationToken);
        var totalReservations = await db.Reservations.IgnoreQueryFilters()
            .CountAsync(cancellationToken);
        var totalReviews = await db.Reviews.IgnoreQueryFilters()
            .CountAsync(cancellationToken);
        var completedPayments = db.Payments.IgnoreQueryFilters()
            .Where(payment => payment.Status == PaymentStatus.Completed
                && payment.Currency == _currency);
        var completedPaymentCount = await completedPayments.CountAsync(cancellationToken);
        var totalRevenue = await completedPayments
            .SumAsync(payment => (decimal?)payment.Amount, cancellationToken)
            ?? 0m;
        var userRoleRows = await db.UserRoles
            .Join(
                db.Roles,
                link => link.RoleId,
                role => role.Id,
                (link, role) => role.Name!)
            .GroupBy(roleName => roleName)
            .Select(group => new { Role = group.Key, Count = group.Count() })
            .OrderBy(item => item.Role)
            .ToListAsync(cancellationToken);
        var usersByRole = userRoleRows
            .Select(item => new RoleCountResponse(item.Role, item.Count))
            .ToArray();
        var reservationStatusRows = await db.Reservations
            .IgnoreQueryFilters()
            .GroupBy(reservation => reservation.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .OrderBy(item => item.Status)
            .ToListAsync(cancellationToken);
        var reservationsByStatus = reservationStatusRows
            .Select(item => new ReservationStatusCountResponse(item.Status, item.Count))
            .ToArray();

        return new AdminStatsResponse(
            totalUsers,
            activeUsers,
            totalProfessionals,
            totalReservations,
            totalReviews,
            completedPaymentCount,
            totalRevenue,
            _currency,
            usersByRole,
            reservationsByStatus,
            DateTime.UtcNow);
    }
}
