using System.Text.Json;
using FixedIT.API.Data;
using FixedIT.API.Models;
using FixedIT.Shared.Messages;
using Microsoft.EntityFrameworkCore;

namespace FixedIT.API.Services;

public sealed class RabbitMqPublisher(
    AppDbContext db) : IReservationEventPublisher, INotificationEventPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task PublishAsync(
        ReservationStatusChangedEvent message,
        CancellationToken cancellationToken)
    {
        var recipientIds = GetReservationRecipientIds(message);
        var recipients = await GetRecipientsAsync(recipientIds, cancellationToken);
        var outgoingMessages = recipients.Select(recipient =>
            (BaseNotificationMessage)new ReservationStatusChangedMessage(
                Guid.NewGuid(),
                recipient.Id,
                recipient.Email,
                recipient.Name,
                message.ChangedAt,
                message.ReservationId,
                message.PreviousStatus?.ToString(),
                message.Status.ToString(),
                message.ChangedByUserId,
                message.Reason));
        Enqueue(outgoingMessages, cancellationToken);
    }

    public async Task PublishNewMessageAsync(
        NewMessageNotificationEvent message,
        CancellationToken cancellationToken)
    {
        var recipientIds = await db.ConversationParticipants
            .Where(participant => participant.ConversationId == message.ConversationId
                && participant.UserId != message.SenderUserId)
            .Select(participant => participant.UserId)
            .ToListAsync(cancellationToken);
        var recipients = await GetRecipientsAsync(recipientIds, cancellationToken);
        var preview = message.Content.Length <= 200
            ? message.Content
            : message.Content[..200];
        var outgoingMessages = recipients.Select(recipient =>
            (BaseNotificationMessage)new NewMessageNotificationMessage(
                Guid.NewGuid(),
                recipient.Id,
                recipient.Email,
                recipient.Name,
                message.SentAt,
                message.ConversationId,
                message.MessageId,
                message.SenderUserId,
                message.SenderName,
                preview));
        Enqueue(outgoingMessages, cancellationToken);
    }

    public async Task PublishPaymentCompletedAsync(
        PaymentCompletedEvent message,
        CancellationToken cancellationToken)
    {
        var recipients = await GetRecipientsAsync(
            [message.ClientUserId],
            cancellationToken);
        var outgoingMessages = recipients.Select(recipient =>
            (BaseNotificationMessage)new PaymentCompletedMessage(
                Guid.NewGuid(),
                recipient.Id,
                recipient.Email,
                recipient.Name,
                message.CompletedAt,
                message.ReservationId,
                message.Amount,
                message.Currency,
                message.PayPalOrderId));
        Enqueue(outgoingMessages, cancellationToken);
    }

    public Task PublishPasswordResetAsync(
        PasswordResetRequestedEvent message,
        CancellationToken cancellationToken)
    {
        Enqueue(
            [new PasswordResetRequestedMessage(
                Guid.NewGuid(),
                message.UserId,
                message.Email,
                message.RecipientName,
                DateTime.UtcNow,
                message.Code,
                message.ExpiresAt)],
            cancellationToken);
        return Task.CompletedTask;
    }

    private void Enqueue(
        IEnumerable<BaseNotificationMessage> messages,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        db.OutboxMessages.AddRange(messages.Select(message => new OutboxMessage
        {
            Id = message.MessageId,
            MessageType = message.GetType().Name,
            Payload = JsonSerializer.Serialize<BaseNotificationMessage>(
                message,
                SerializerOptions),
            OccurredAt = message.OccurredAt,
            CreatedAt = DateTime.UtcNow
        }));
    }

    private async Task<IReadOnlyCollection<NotificationRecipient>> GetRecipientsAsync(
        IEnumerable<string> userIds,
        CancellationToken cancellationToken)
    {
        var uniqueIds = userIds.Distinct(StringComparer.Ordinal).ToArray();
        return await db.Users
            .AsNoTracking()
            .Where(user => uniqueIds.Contains(user.Id)
                && user.IsActive
                && user.Email != null)
            .Select(user => new NotificationRecipient(
                user.Id,
                user.Email!,
                user.FirstName + " " + user.LastName))
            .ToListAsync(cancellationToken);
    }

    private static IReadOnlyCollection<string> GetReservationRecipientIds(
        ReservationStatusChangedEvent message)
    {
        var recipients = new HashSet<string>(StringComparer.Ordinal);
        if (message.ClientUserId != message.ChangedByUserId)
        {
            recipients.Add(message.ClientUserId);
        }

        if (message.ProfessionalUserId != message.ChangedByUserId)
        {
            recipients.Add(message.ProfessionalUserId);
        }

        return recipients;
    }

    private sealed record NotificationRecipient(string Id, string Email, string Name);
}
