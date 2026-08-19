using FixedIT.API.Models.Enums;

namespace FixedIT.API.Models;

public class Payment
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public string PayPalOrderId { get; set; } = string.Empty;
    public string? PayPalCaptureId { get; set; }
    public string? PayPalRefundId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime? RefundedAt { get; set; }

    public Reservation Reservation { get; set; } = null!;
}
