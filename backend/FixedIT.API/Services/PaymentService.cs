using System.Data;
using System.Globalization;
using System.Text.Json;
using FixedIT.API.Configuration;
using FixedIT.API.Constants;
using FixedIT.API.CustomExceptions;
using FixedIT.API.Data;
using FixedIT.API.DTOs.Payments;
using FixedIT.API.Models;
using FixedIT.API.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FixedIT.API.Services;

internal sealed class PaymentService(
    AppDbContext db,
    IPayPalService payPalService,
    IOptions<PayPalOptions> options,
    INotificationService notificationService,
    INotificationEventPublisher notificationEventPublisher,
    ILogger<PaymentService> logger) : IPaymentService, IPaymentRefundService
{
    private const string LockedReservationsSql =
        "SELECT * FROM [Reservations] WITH (UPDLOCK, HOLDLOCK)";
    private const string CompletedStatus = "COMPLETED";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly PayPalOptions _options = options.Value;

    public async Task<PaymentOrderResponse> CreateOrderAsync(
        string userId,
        int reservationId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var reservation = await GetLockedReservationAsync(
            item => item.Id == reservationId,
            cancellationToken);
        if (reservation.ClientUserId != userId)
        {
            throw new NotFoundException("Rezervacija nije pronađena.");
        }

        if (reservation.Status != ReservationStatus.Completed)
        {
            throw new BusinessException("Plaćanje je dostupno tek nakon završetka rezervacije.");
        }

        if (reservation.Payment is not null)
        {
            if (reservation.Payment.Status is not PaymentStatus.Pending)
            {
                throw new BusinessException("Ova rezervacija već ima završeno plaćanje.");
            }

            var existingOrder = await payPalService.GetOrderAsync(
                reservation.Payment.PayPalOrderId,
                cancellationToken);
            ValidateOrderIdentity(existingOrder, reservation.Payment.PayPalOrderId);
            if (string.IsNullOrWhiteSpace(existingOrder.ApprovalUrl))
            {
                throw new BusinessException("Postojeća PayPal narudžba više ne čeka odobrenje.");
            }

            return MapOrder(reservation.Payment, existingOrder.ApprovalUrl);
        }

        if (reservation.TotalPrice <= 0)
        {
            throw new BusinessException("Ukupna cijena rezervacije mora biti veća od nule.");
        }

        var order = await payPalService.CreateOrderAsync(
            reservation.Id,
            reservation.TotalPrice,
            _options.Currency,
            BuildRequestId("order", reservation.Id),
            cancellationToken);
        if (string.IsNullOrWhiteSpace(order.ApprovalUrl))
        {
            throw new BusinessException("PayPal nije vratio adresu za odobrenje plaćanja.");
        }

        var payment = new Payment
        {
            ReservationId = reservation.Id,
            PayPalOrderId = order.OrderId,
            Amount = reservation.TotalPrice,
            Currency = _options.Currency,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return MapOrder(payment, order.ApprovalUrl);
    }

    public async Task<PaymentResponse> CaptureOrderAsync(
        string userId,
        string orderId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            throw new BusinessException("Identifikator PayPal narudžbe je obavezan.");
        }

        if (orderId.Length > DatabaseConstants.ExternalIdMaxLength)
        {
            throw new BusinessException("Identifikator PayPal narudžbe nije ispravan.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var reservation = await GetLockedReservationAsync(
            item => item.Payment != null && item.Payment.PayPalOrderId == orderId,
            cancellationToken);
        if (reservation.ClientUserId != userId)
        {
            throw new NotFoundException("Plaćanje nije pronađeno.");
        }

        var payment = reservation.Payment!;
        if (reservation.Status != ReservationStatus.Completed)
        {
            throw new BusinessException("Plaćanje je dostupno tek nakon završetka rezervacije.");
        }

        if (payment.Status == PaymentStatus.Completed)
        {
            return Map(payment);
        }

        if (payment.Status == PaymentStatus.Refunded)
        {
            throw new BusinessException("Refundirano plaćanje nije moguće ponovo naplatiti.");
        }

        var capture = await payPalService.CaptureOrderAsync(
            payment.PayPalOrderId,
            BuildRequestId("capture", payment.Id),
            cancellationToken);
        ValidateCapture(payment, capture);
        var completion = CompletePayment(
            payment,
            capture.CaptureId,
            DateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await DispatchCompletionAsync(completion, cancellationToken);
        return Map(payment);
    }

    public async Task<bool> HandleWebhookAsync(
        PayPalWebhookHeaders headers,
        string rawBody,
        CancellationToken cancellationToken)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(rawBody);
        }
        catch (JsonException)
        {
            throw new BusinessException("Sadržaj PayPal webhook zahtjeva nije ispravan.");
        }

        using (document)
        {
            var root = document.RootElement;
            bool verified;
            try
            {
                verified = await payPalService.VerifyWebhookSignatureAsync(
                    headers,
                    root,
                    cancellationToken);
            }
            catch (BusinessException exception)
            {
                logger.LogWarning(
                    exception,
                    "PayPal webhook signature verification failed for transmission {TransmissionId}.",
                    headers.TransmissionId);
                return false;
            }

            if (!verified)
            {
                logger.LogWarning(
                    "Rejected PayPal webhook transmission {TransmissionId} because signature verification failed.",
                    headers.TransmissionId);
                return false;
            }

            var eventType = GetRequiredString(root, "event_type");
            if (!string.Equals(
                eventType,
                "PAYMENT.CAPTURE.COMPLETED",
                StringComparison.Ordinal))
            {
                return true;
            }

            WebhookCaptureConfirmation confirmation;
            try
            {
                confirmation = ParseWebhookCapture(root.GetProperty("resource"));
            }
            catch (Exception exception) when (exception is KeyNotFoundException
                or InvalidOperationException
                or IndexOutOfRangeException
                or JsonException)
            {
                throw new BusinessException("PayPal webhook ne sadrži obavezne podatke.");
            }

            await CompleteFromWebhookAsync(confirmation, cancellationToken);
            return true;
        }
    }

    public async Task<PaymentResponse> RefundByIdAsync(
        string userId,
        int paymentId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var reservation = await GetLockedReservationAsync(
            item => item.Payment != null && item.Payment.Id == paymentId,
            cancellationToken);
        if (!await IsAdminAsync(userId, cancellationToken))
        {
            throw new NotFoundException("Plaćanje nije pronađeno.");
        }

        var payment = reservation.Payment!;
        if (payment.Status == PaymentStatus.Pending)
        {
            throw new BusinessException("Plaćanje na čekanju nije moguće refundirati.");
        }

        await RefundAsync(payment, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(payment);
    }

    public async Task RefundAsync(Payment payment, CancellationToken cancellationToken)
    {
        if (payment.Status is PaymentStatus.Pending or PaymentStatus.Refunded)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(payment.PayPalCaptureId))
        {
            throw new BusinessException("Završeno plaćanje nema identifikator PayPal naplate.");
        }

        var refund = await payPalService.RefundAsync(
            payment.PayPalCaptureId,
            payment.Amount,
            payment.Currency,
            BuildRequestId("refund", payment.Id),
            cancellationToken);
        if (!string.Equals(refund.Status, CompletedStatus, StringComparison.Ordinal)
            || refund.Amount != payment.Amount
            || !string.Equals(refund.Currency, payment.Currency, StringComparison.Ordinal))
        {
            throw new BusinessException("Podaci PayPal refundacije ne odgovaraju plaćanju.");
        }

        payment.PayPalRefundId = refund.RefundId;
        payment.Status = PaymentStatus.Refunded;
        payment.RefundedAt = DateTime.UtcNow;
    }

    private async Task CompleteFromWebhookAsync(
        WebhookCaptureConfirmation confirmation,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var reservation = await GetLockedReservationAsync(
            item => item.Payment != null
                && item.Payment.PayPalOrderId == confirmation.OrderId,
            cancellationToken);
        var payment = reservation.Payment!;
        ValidateWebhookCapture(payment, confirmation);
        if (payment.Status is PaymentStatus.Completed or PaymentStatus.Refunded)
        {
            if (!string.Equals(
                payment.PayPalCaptureId,
                confirmation.CaptureId,
                StringComparison.Ordinal))
            {
                throw new BusinessException("Identifikator naplate iz PayPal webhooka ne odgovara plaćanju.");
            }

            return;
        }

        var completion = CompletePayment(
            payment,
            confirmation.CaptureId,
            confirmation.CompletedAt);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await DispatchCompletionAsync(completion, cancellationToken);
    }

    private PaymentCompletion CompletePayment(
        Payment payment,
        string captureId,
        DateTime completedAt)
    {
        payment.PayPalCaptureId = captureId;
        payment.Status = PaymentStatus.Completed;
        payment.CompletedAt = completedAt;
        var notification = new Notification
        {
            UserId = payment.Reservation.ClientUserId,
            Title = "Plaćanje potvrđeno",
            Body = $"Plaćanje za rezervaciju #{payment.ReservationId} je uspješno potvrđeno.",
            IsRead = false,
            CreatedAt = completedAt,
            Type = NotificationType.Payment
        };
        db.Notifications.Add(notification);
        return new PaymentCompletion(payment, notification);
    }

    private async Task DispatchCompletionAsync(
        PaymentCompletion completion,
        CancellationToken cancellationToken)
    {
        try
        {
            await notificationService.PushAsync(completion.Notification, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to push in-app notification for completed payment {PaymentId}.",
                completion.Payment.Id);
        }

        try
        {
            await notificationEventPublisher.PublishPaymentCompletedAsync(
                new PaymentCompletedEvent(
                    completion.Payment.ReservationId,
                    completion.Payment.Reservation.ClientUserId,
                    completion.Payment.Amount,
                    completion.Payment.Currency,
                    completion.Payment.PayPalOrderId,
                    completion.Payment.CompletedAt!.Value),
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to publish notification event for completed payment {PaymentId}.",
                completion.Payment.Id);
        }
    }

    private async Task<Reservation> GetLockedReservationAsync(
        System.Linq.Expressions.Expression<Func<Reservation, bool>> predicate,
        CancellationToken cancellationToken)
    {
        return await db.Reservations
            .FromSqlRaw(LockedReservationsSql)
            .IgnoreQueryFilters()
            .Include(item => item.ClientUser)
            .Include(item => item.Payment)
            .SingleOrDefaultAsync(predicate, cancellationToken)
            ?? throw new NotFoundException("Plaćanje nije pronađeno.");
    }

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

    private static void ValidateOrderIdentity(PayPalOrderResult order, string expectedOrderId)
    {
        if (!string.Equals(order.OrderId, expectedOrderId, StringComparison.Ordinal))
        {
            throw new BusinessException("Identifikator PayPal narudžbe ne odgovara plaćanju.");
        }
    }

    private static void ValidateCapture(Payment payment, PayPalCaptureResult capture)
    {
        if (!string.Equals(capture.OrderId, payment.PayPalOrderId, StringComparison.Ordinal)
            || !string.Equals(capture.Status, CompletedStatus, StringComparison.Ordinal)
            || !string.Equals(capture.CaptureStatus, CompletedStatus, StringComparison.Ordinal)
            || capture.ReservationReference != payment.ReservationId.ToString(CultureInfo.InvariantCulture)
            || capture.Amount != payment.Amount
            || !string.Equals(capture.Currency, payment.Currency, StringComparison.Ordinal))
        {
            throw new BusinessException("Podaci PayPal naplate ne odgovaraju plaćanju.");
        }
    }

    private static void ValidateWebhookCapture(
        Payment payment,
        WebhookCaptureConfirmation confirmation)
    {
        if (!string.Equals(confirmation.Status, CompletedStatus, StringComparison.Ordinal)
            || confirmation.Amount != payment.Amount
            || !string.Equals(confirmation.Currency, payment.Currency, StringComparison.Ordinal)
            || (confirmation.ReservationReference is not null
                && confirmation.ReservationReference
                    != payment.ReservationId.ToString(CultureInfo.InvariantCulture)))
        {
            throw new BusinessException("Podaci PayPal webhooka ne odgovaraju plaćanju.");
        }
    }

    private static WebhookCaptureConfirmation ParseWebhookCapture(JsonElement resource)
    {
        var amount = resource.GetProperty("amount");
        var relatedIds = resource
            .GetProperty("supplementary_data")
            .GetProperty("related_ids");
        string? reservationReference = null;
        if (resource.TryGetProperty("custom_id", out var customId))
        {
            reservationReference = customId.GetString();
        }

        var completedAt = DateTime.UtcNow;
        if (resource.TryGetProperty("update_time", out var updateTime)
            && DateTime.TryParse(
                updateTime.GetString(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out var parsedTime))
        {
            completedAt = parsedTime.ToUniversalTime();
        }

        return new WebhookCaptureConfirmation(
            GetRequiredString(resource, "id"),
            GetRequiredString(resource, "status"),
            GetRequiredString(relatedIds, "order_id"),
            ParseAmount(amount),
            GetRequiredString(amount, "currency_code"),
            reservationReference,
            completedAt);
    }

    private static decimal ParseAmount(JsonElement amount)
    {
        if (!decimal.TryParse(
            GetRequiredString(amount, "value"),
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var parsed))
        {
            throw new BusinessException("Iznos iz PayPal webhooka nije ispravan.");
        }

        return parsed;
    }

    private static string GetRequiredString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property)
            || string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new BusinessException("PayPal webhook ne sadrži obavezne podatke.");
        }

        return property.GetString()!;
    }

    private static PaymentOrderResponse MapOrder(Payment payment, string approvalUrl)
    {
        return new PaymentOrderResponse(
            payment.Id,
            payment.ReservationId,
            payment.PayPalOrderId,
            approvalUrl,
            payment.Amount,
            payment.Currency,
            payment.Status);
    }

    private static PaymentResponse Map(Payment payment)
    {
        return new PaymentResponse(
            payment.Id,
            payment.ReservationId,
            payment.PayPalOrderId,
            payment.Amount,
            payment.Currency,
            payment.Status,
            payment.CreatedAt,
            payment.CompletedAt,
            payment.RefundedAt);
    }

    private static string BuildRequestId(string operation, int entityId)
    {
        return $"fixedit-{operation}-{entityId}";
    }

    private sealed record PaymentCompletion(Payment Payment, Notification Notification);

    private sealed record WebhookCaptureConfirmation(
        string CaptureId,
        string Status,
        string OrderId,
        decimal Amount,
        string Currency,
        string? ReservationReference,
        DateTime CompletedAt);
}
