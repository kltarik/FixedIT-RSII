using System.ComponentModel.DataAnnotations;
using FixedIT.API.Models.Enums;

namespace FixedIT.API.DTOs.Payments;

public sealed class CreatePaymentOrderRequest
{
    [Range(1, int.MaxValue)]
    public int ReservationId { get; set; }
}

public sealed record PaymentOrderResponse(
    int PaymentId,
    int ReservationId,
    string OrderId,
    string ApprovalUrl,
    decimal Amount,
    string Currency,
    PaymentStatus Status);

public sealed record PaymentResponse(
    int Id,
    int ReservationId,
    string OrderId,
    decimal Amount,
    string Currency,
    PaymentStatus Status,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    DateTime? RefundedAt);
