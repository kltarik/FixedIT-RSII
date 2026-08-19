using FixedIT.API.DTOs.Payments;
using FixedIT.API.Models;

namespace FixedIT.API.Services;

public sealed record PayPalWebhookHeaders(
    string TransmissionId,
    string TransmissionTime,
    string CertificateUrl,
    string AuthenticationAlgorithm,
    string TransmissionSignature);

public interface IPaymentService
{
    Task<PaymentOrderResponse> CreateOrderAsync(
        string userId,
        int reservationId,
        CancellationToken cancellationToken);

    Task<PaymentResponse> CaptureOrderAsync(
        string userId,
        string orderId,
        CancellationToken cancellationToken);

    Task<bool> HandleWebhookAsync(
        PayPalWebhookHeaders headers,
        string rawBody,
        CancellationToken cancellationToken);

    Task<PaymentResponse> RefundByIdAsync(
        string userId,
        int paymentId,
        CancellationToken cancellationToken);
}
