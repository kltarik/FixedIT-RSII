using FixedIT.API.Configuration;
using FixedIT.API.Data;
using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Professionals;
using FixedIT.API.DTOs.Reports;
using FixedIT.API.Models;
using FixedIT.API.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FixedIT.API.Services;

public sealed class ReportService(
    AppDbContext db,
    IPaginationService paginationService,
    IOptions<PayPalOptions> payPalOptions,
    IOptions<ReportOptions> reportOptions) : IReportService
{
    private readonly string _currency = payPalOptions.Value.Currency;
    private readonly ReportOptions _reportOptions = reportOptions.Value;

    public async Task<FinancialReportResponse> GetFinancialReportAsync(
        string userId,
        bool isAdmin,
        ReportFilterRequest filters,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        var page = paginationService.Normalize(request);
        var query = BuildCompletedPaymentQuery(userId, isAdmin, filters);
        var paymentCount = await query.CountAsync(cancellationToken);
        var totalRevenue = await query
            .SumAsync(payment => (decimal?)payment.Amount, cancellationToken)
            ?? 0m;
        var monthlyRevenueRows = await query
            .GroupBy(payment => new
            {
                Year = payment.CompletedAt!.Value.Year,
                Month = payment.CompletedAt.Value.Month
            })
            .Select(group => new MonthlyRevenueProjection
            {
                Year = group.Key.Year,
                Month = group.Key.Month,
                Revenue = group.Sum(payment => payment.Amount),
                PaymentCount = group.Count()
            })
            .OrderBy(item => item.Year)
            .ThenBy(item => item.Month)
            .ToListAsync(cancellationToken);
        var revenueByMonth = monthlyRevenueRows
            .Select(item => new MonthlyRevenueResponse(
                item.Year,
                item.Month,
                item.Revenue,
                item.PaymentCount))
            .ToArray();
        var categoryRevenueRows = await BuildCategoryRevenueQuery(query)
            .ToListAsync(cancellationToken);
        var revenueByCategory = categoryRevenueRows
            .Select(item => new CategoryRevenueResponse(
                item.CategoryId,
                item.CategoryName,
                item.Revenue,
                item.PaymentCount))
            .ToArray();
        var reservations = await ProjectFinancialReservations(query)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new FinancialReportResponse(
            filters.From,
            filters.To,
            filters.CategoryId,
            _currency,
            totalRevenue,
            paymentCount,
            revenueByMonth,
            revenueByCategory,
            new PagedResponse<FinancialReservationResponse>(
                reservations,
                paymentCount,
                page.Page,
                page.PageSize));
    }

    public async Task<FinancialReportDocumentData> GetFinancialReportDocumentAsync(
        string userId,
        bool isAdmin,
        ReportFilterRequest filters,
        CancellationToken cancellationToken)
    {
        var query = BuildCompletedPaymentQuery(userId, isAdmin, filters);
        var paymentCount = await query.CountAsync(cancellationToken);
        var totalRevenue = await query
            .SumAsync(payment => (decimal?)payment.Amount, cancellationToken)
            ?? 0m;
        var reservations = await ProjectFinancialReservations(query)
            .Take(_reportOptions.MaxPdfRows + 1)
            .ToListAsync(cancellationToken);
        var isTruncated = reservations.Count > _reportOptions.MaxPdfRows;
        if (isTruncated)
        {
            reservations.RemoveAt(reservations.Count - 1);
        }

        return new FinancialReportDocumentData(
            filters.From,
            filters.To,
            filters.CategoryId,
            _currency,
            totalRevenue,
            paymentCount,
            reservations,
            isTruncated,
            DateTime.UtcNow);
    }

    public async Task<PagedResponse<ProfessionalPerformanceResponse>>
        GetProfessionalPerformanceAsync(
            ReportFilterRequest filters,
            PagedRequest request,
            CancellationToken cancellationToken)
    {
        var page = paginationService.Normalize(request);
        var performanceQuery = BuildProfessionalPerformanceQuery(filters);
        var total = await performanceQuery.CountAsync(cancellationToken);
        var rows = await performanceQuery
            .OrderByDescending(item => item.TotalRevenue)
            .ThenByDescending(item => item.CompletedReservations)
            .ThenBy(item => item.LastName)
            .ThenBy(item => item.FirstName)
            .ThenBy(item => item.ProfessionalProfileId)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);
        var items = rows.Select(MapProfessionalPerformance).ToArray();

        return new PagedResponse<ProfessionalPerformanceResponse>(
            items,
            total,
            page.Page,
            page.PageSize);
    }

    public async Task<ProfessionalPerformanceDocumentData>
        GetProfessionalPerformanceDocumentAsync(
            ReportFilterRequest filters,
            CancellationToken cancellationToken)
    {
        var rows = await BuildProfessionalPerformanceQuery(filters)
            .OrderByDescending(item => item.TotalRevenue)
            .ThenByDescending(item => item.CompletedReservations)
            .ThenBy(item => item.LastName)
            .ThenBy(item => item.FirstName)
            .ThenBy(item => item.ProfessionalProfileId)
            .Take(_reportOptions.MaxPdfRows + 1)
            .ToListAsync(cancellationToken);
        var isTruncated = rows.Count > _reportOptions.MaxPdfRows;
        if (isTruncated)
        {
            rows.RemoveAt(rows.Count - 1);
        }

        return new ProfessionalPerformanceDocumentData(
            filters.From,
            filters.To,
            filters.CategoryId,
            _currency,
            rows.Select(MapProfessionalPerformance).ToArray(),
            isTruncated,
            DateTime.UtcNow);
    }

    private IQueryable<Payment> BuildCompletedPaymentQuery(
        string userId,
        bool isAdmin,
        ReportFilterRequest filters)
    {
        var dateRange = CreateDateRange(filters);
        var query = db.Payments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(payment => payment.Status == PaymentStatus.Completed
                && payment.CompletedAt.HasValue
                && payment.Currency == _currency);
        if (!isAdmin)
        {
            query = query.Where(payment =>
                payment.Reservation.ProfessionalProfile.UserId == userId);
        }

        if (filters.CategoryId.HasValue)
        {
            query = query.Where(payment =>
                payment.Reservation.CategoryId == filters.CategoryId.Value);
        }

        if (dateRange.FromUtc.HasValue)
        {
            query = query.Where(payment =>
                payment.CompletedAt!.Value >= dateRange.FromUtc.Value);
        }

        if (dateRange.ToExclusiveUtc.HasValue)
        {
            query = query.Where(payment =>
                payment.CompletedAt!.Value < dateRange.ToExclusiveUtc.Value);
        }

        return query;
    }

    private static IQueryable<CategoryRevenueProjection> BuildCategoryRevenueQuery(
        IQueryable<Payment> query)
    {
        return query
            .Select(payment => new
            {
                payment.Reservation.CategoryId,
                payment.Reservation.Category.Name,
                payment.Amount
            })
            .GroupBy(item => new { item.CategoryId, item.Name })
            .Select(group => new CategoryRevenueProjection
            {
                CategoryId = group.Key.CategoryId,
                CategoryName = group.Key.Name,
                Revenue = group.Sum(item => item.Amount),
                PaymentCount = group.Count()
            })
            .OrderByDescending(item => item.Revenue)
            .ThenBy(item => item.CategoryName)
            .ThenBy(item => item.CategoryId);
    }

    private static IQueryable<FinancialReservationResponse> ProjectFinancialReservations(
        IQueryable<Payment> query)
    {
        return query
            .OrderByDescending(payment => payment.CompletedAt)
            .ThenByDescending(payment => payment.Id)
            .Select(payment => new FinancialReservationResponse(
                payment.Id,
                payment.ReservationId,
                payment.Reservation.ProfessionalProfileId,
                payment.CompletedAt!.Value,
                payment.Reservation.ClientUser.FirstName + " "
                    + payment.Reservation.ClientUser.LastName,
                payment.Reservation.ProfessionalProfile.User.FirstName + " "
                    + payment.Reservation.ProfessionalProfile.User.LastName,
                payment.Amount,
                payment.Currency,
                payment.Reservation.ProfessionalProfile.ProfessionalCategories
                    .Where(link => link.CategoryId == payment.Reservation.CategoryId)
                    .Select(link => new CategorySummaryResponse(
                        payment.Reservation.CategoryId,
                        payment.Reservation.Category.Name))
                    .ToArray()));
    }

    private IQueryable<ProfessionalPerformanceProjection>
        BuildProfessionalPerformanceQuery(ReportFilterRequest filters)
    {
        var dateRange = CreateDateRange(filters);
        var profiles = db.ProfessionalProfiles
            .IgnoreQueryFilters()
            .AsNoTracking();
        if (filters.CategoryId.HasValue)
        {
            profiles = profiles.Where(profile => profile.ProfessionalCategories
                .Any(link => link.CategoryId == filters.CategoryId.Value));
        }

        return profiles.Select(profile => new ProfessionalPerformanceProjection
        {
            ProfessionalProfileId = profile.Id,
            UserId = profile.UserId,
            FirstName = profile.User.FirstName,
            LastName = profile.User.LastName,
            AverageRating = profile.AverageRating,
            ReviewCount = profile.Reviews.Count(review =>
                (!dateRange.FromUtc.HasValue || review.CreatedAt >= dateRange.FromUtc.Value)
                && (!dateRange.ToExclusiveUtc.HasValue
                    || review.CreatedAt < dateRange.ToExclusiveUtc.Value)),
            TotalReservations = profile.Reservations.Count(reservation =>
                (!dateRange.FromUtc.HasValue || reservation.ScheduledAt >= dateRange.FromUtc.Value)
                && (!dateRange.ToExclusiveUtc.HasValue
                    || reservation.ScheduledAt < dateRange.ToExclusiveUtc.Value)),
            CompletedReservations = profile.Reservations.Count(reservation =>
                reservation.Status == ReservationStatus.Completed
                && (!dateRange.FromUtc.HasValue || reservation.ScheduledAt >= dateRange.FromUtc.Value)
                && (!dateRange.ToExclusiveUtc.HasValue
                    || reservation.ScheduledAt < dateRange.ToExclusiveUtc.Value)),
            CancelledReservations = profile.Reservations.Count(reservation =>
                reservation.Status == ReservationStatus.Cancelled
                && (!dateRange.FromUtc.HasValue || reservation.ScheduledAt >= dateRange.FromUtc.Value)
                && (!dateRange.ToExclusiveUtc.HasValue
                    || reservation.ScheduledAt < dateRange.ToExclusiveUtc.Value)),
            TotalRevenue = profile.Reservations
                .Where(reservation => reservation.Payment != null
                    && reservation.Payment.Status == PaymentStatus.Completed
                    && reservation.Payment.Currency == _currency
                    && reservation.Payment.CompletedAt.HasValue
                    && (!dateRange.FromUtc.HasValue
                        || reservation.Payment.CompletedAt.Value >= dateRange.FromUtc.Value)
                    && (!dateRange.ToExclusiveUtc.HasValue
                        || reservation.Payment.CompletedAt.Value < dateRange.ToExclusiveUtc.Value))
                .Sum(reservation => (decimal?)reservation.Payment!.Amount) ?? 0m
        });
    }

    private ProfessionalPerformanceResponse MapProfessionalPerformance(
        ProfessionalPerformanceProjection item)
    {
        return new ProfessionalPerformanceResponse(
            item.ProfessionalProfileId,
            item.UserId,
            item.FirstName,
            item.LastName,
            item.AverageRating,
            item.ReviewCount,
            item.TotalReservations,
            item.CompletedReservations,
            item.CancelledReservations,
            item.TotalReservations == 0
                ? 0m
                : decimal.Round(
                    item.CompletedReservations * 100m / item.TotalReservations,
                    2,
                    MidpointRounding.AwayFromZero),
            item.TotalRevenue,
            _currency);
    }

    private static ReportDateRange CreateDateRange(ReportFilterRequest filters)
    {
        var fromUtc = filters.From?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        DateTime? toExclusiveUtc = null;
        if (filters.To.HasValue && filters.To.Value != DateOnly.MaxValue)
        {
            toExclusiveUtc = filters.To.Value
                .AddDays(1)
                .ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        }

        return new ReportDateRange(fromUtc, toExclusiveUtc);
    }

    private sealed record ReportDateRange(DateTime? FromUtc, DateTime? ToExclusiveUtc);

    private sealed class MonthlyRevenueProjection
    {
        public int Year { get; init; }
        public int Month { get; init; }
        public decimal Revenue { get; init; }
        public int PaymentCount { get; init; }
    }

    private sealed class CategoryRevenueProjection
    {
        public int CategoryId { get; init; }
        public string CategoryName { get; init; } = string.Empty;
        public decimal Revenue { get; init; }
        public int PaymentCount { get; init; }
    }

    private sealed class ProfessionalPerformanceProjection
    {
        public int ProfessionalProfileId { get; init; }
        public string UserId { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public decimal AverageRating { get; init; }
        public int ReviewCount { get; init; }
        public int TotalReservations { get; init; }
        public int CompletedReservations { get; init; }
        public int CancelledReservations { get; init; }
        public decimal TotalRevenue { get; init; }
    }
}
