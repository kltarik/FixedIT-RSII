using System.ComponentModel.DataAnnotations;
using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Professionals;
using FixedIT.API.Models.Enums;

namespace FixedIT.API.DTOs.Reports;

public sealed class ReportFilterRequest : IValidatableObject
{
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }

    [Range(1, int.MaxValue)]
    public int? CategoryId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From.HasValue && To.HasValue && From > To)
        {
            yield return new ValidationResult(
                "Početni datum ne može biti poslije završnog datuma.",
                [nameof(From), nameof(To)]);
        }
    }
}

public sealed record MonthlyRevenueResponse(
    int Year,
    int Month,
    decimal Revenue,
    int PaymentCount);

public sealed record CategoryRevenueResponse(
    int CategoryId,
    string CategoryName,
    decimal Revenue,
    int PaymentCount);

public sealed record FinancialReservationResponse(
    int PaymentId,
    int ReservationId,
    int ProfessionalProfileId,
    DateTime CompletedAt,
    string ClientName,
    string ProfessionalName,
    decimal Amount,
    string Currency,
    IReadOnlyCollection<CategorySummaryResponse> Categories);

public sealed record FinancialReportResponse(
    DateOnly? From,
    DateOnly? To,
    int? CategoryId,
    string Currency,
    decimal TotalRevenue,
    int PaymentCount,
    IReadOnlyCollection<MonthlyRevenueResponse> RevenueByMonth,
    IReadOnlyCollection<CategoryRevenueResponse> RevenueByCategory,
    PagedResponse<FinancialReservationResponse> Reservations);

public sealed record FinancialReportDocumentData(
    DateOnly? From,
    DateOnly? To,
    int? CategoryId,
    string Currency,
    decimal TotalRevenue,
    int PaymentCount,
    IReadOnlyCollection<FinancialReservationResponse> Reservations,
    bool IsTruncated,
    DateTime GeneratedAtUtc);

public sealed record ProfessionalPerformanceResponse(
    int ProfessionalProfileId,
    string UserId,
    string FirstName,
    string LastName,
    decimal AverageRating,
    int ReviewCount,
    int TotalReservations,
    int CompletedReservations,
    int CancelledReservations,
    decimal CompletionRate,
    decimal TotalRevenue,
    string Currency);

public sealed record ReservationStatusCountResponse(
    ReservationStatus Status,
    int Count);

public sealed record RoleCountResponse(string Role, int Count);

public sealed record AdminStatsResponse(
    int TotalUsers,
    int ActiveUsers,
    int TotalProfessionals,
    int TotalReservations,
    int TotalReviews,
    int CompletedPayments,
    decimal TotalRevenue,
    string Currency,
    IReadOnlyCollection<RoleCountResponse> UsersByRole,
    IReadOnlyCollection<ReservationStatusCountResponse> ReservationsByStatus,
    DateTime GeneratedAtUtc);
