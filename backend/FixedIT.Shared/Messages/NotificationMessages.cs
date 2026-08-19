using System.Text.Json.Serialization;

namespace FixedIT.Shared.Messages;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$messageType")]
[JsonDerivedType(typeof(ReservationStatusChangedMessage), "reservation-status-changed")]
[JsonDerivedType(typeof(NewMessageNotificationMessage), "new-chat-message")]
[JsonDerivedType(typeof(PaymentCompletedMessage), "payment-completed")]
public abstract record BaseNotificationMessage(
    Guid MessageId,
    string RecipientUserId,
    string RecipientEmail,
    string RecipientName,
    DateTime OccurredAt);

public sealed record ReservationStatusChangedMessage(
    Guid MessageId,
    string RecipientUserId,
    string RecipientEmail,
    string RecipientName,
    DateTime OccurredAt,
    int ReservationId,
    string? PreviousStatus,
    string Status,
    string ChangedByUserId,
    string? Reason) : BaseNotificationMessage(
        MessageId,
        RecipientUserId,
        RecipientEmail,
        RecipientName,
        OccurredAt);

public sealed record NewMessageNotificationMessage(
    Guid MessageId,
    string RecipientUserId,
    string RecipientEmail,
    string RecipientName,
    DateTime OccurredAt,
    int ConversationId,
    int ChatMessageId,
    string SenderUserId,
    string SenderName,
    string ContentPreview) : BaseNotificationMessage(
        MessageId,
        RecipientUserId,
        RecipientEmail,
        RecipientName,
        OccurredAt);

public sealed record PaymentCompletedMessage(
    Guid MessageId,
    string RecipientUserId,
    string RecipientEmail,
    string RecipientName,
    DateTime OccurredAt,
    int ReservationId,
    decimal Amount,
    string Currency,
    string PayPalOrderId) : BaseNotificationMessage(
        MessageId,
        RecipientUserId,
        RecipientEmail,
        RecipientName,
        OccurredAt);
