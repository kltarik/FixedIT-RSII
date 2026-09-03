namespace FixedIT.API.Services;

public sealed record NewMessageNotificationEvent(
    int ConversationId,
    int MessageId,
    string SenderUserId,
    string SenderName,
    string Content,
    DateTime SentAt);

public sealed record PaymentCompletedEvent(
    int ReservationId,
    string ClientUserId,
    decimal Amount,
    string Currency,
    string PayPalOrderId,
    DateTime CompletedAt);

public sealed record PasswordResetRequestedEvent(
    string UserId,
    string Email,
    string RecipientName,
    string Code,
    DateTime ExpiresAt);

public interface INotificationEventPublisher
{
    Task PublishNewMessageAsync(
        NewMessageNotificationEvent message,
        CancellationToken cancellationToken);

    Task PublishPaymentCompletedAsync(
        PaymentCompletedEvent message,
        CancellationToken cancellationToken);

    Task PublishPasswordResetAsync(
        PasswordResetRequestedEvent message,
        CancellationToken cancellationToken);
}
