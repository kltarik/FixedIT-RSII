using System.Text;
using System.Text.Json;
using FixedIT.API.Data;
using FixedIT.Shared.Configuration;
using FixedIT.Shared.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace FixedIT.API.Services;

public sealed class RabbitMqPublisher(
    AppDbContext db,
    IConnection connection,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqPublisher> logger) : IReservationEventPublisher, INotificationEventPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqOptions _options = options.Value;

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
        Publish(outgoingMessages, cancellationToken);
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
        Publish(outgoingMessages, cancellationToken);
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
        Publish(outgoingMessages, cancellationToken);
    }

    private void Publish(
        IEnumerable<BaseNotificationMessage> messages,
        CancellationToken cancellationToken)
    {
        var outgoingMessages = messages.ToArray();
        if (outgoingMessages.Length == 0)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        using var channel = connection.CreateModel();
        DeclareTopology(channel);
        channel.ConfirmSelect();
        foreach (var message in outgoingMessages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.ContentEncoding = "utf-8";
            properties.MessageId = message.MessageId.ToString("D");
            properties.Type = message.GetType().Name;
            properties.Timestamp = new AmqpTimestamp(
                new DateTimeOffset(message.OccurredAt).ToUnixTimeSeconds());
            var body = Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize<BaseNotificationMessage>(message, SerializerOptions));
            channel.BasicPublish(
                _options.ExchangeName,
                _options.RoutingKey,
                mandatory: true,
                properties,
                body);
        }

        channel.WaitForConfirmsOrDie(
            TimeSpan.FromSeconds(_options.PublishConfirmTimeoutSeconds));
        logger.LogInformation(
            "Published {MessageCount} notification event(s) to RabbitMQ.",
            outgoingMessages.Length);
    }

    private async Task<IReadOnlyCollection<NotificationRecipient>> GetRecipientsAsync(
        IEnumerable<string> userIds,
        CancellationToken cancellationToken)
    {
        var uniqueIds = userIds.Distinct(StringComparer.Ordinal).ToArray();
        return await db.Users
            .AsNoTracking()
            .Where(user => uniqueIds.Contains(user.Id) && user.Email != null)
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

    private void DeclareTopology(IModel channel)
    {
        channel.ExchangeDeclare(
            _options.ExchangeName,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false);
        channel.ExchangeDeclare(
            _options.DeadLetterExchangeName,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false);
        channel.QueueDeclare(
            _options.DeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false);
        channel.QueueBind(
            _options.DeadLetterQueueName,
            _options.DeadLetterExchangeName,
            _options.DeadLetterRoutingKey);
        channel.QueueDeclare(
            _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = _options.DeadLetterExchangeName,
                ["x-dead-letter-routing-key"] = _options.DeadLetterRoutingKey
            });
        channel.QueueBind(
            _options.QueueName,
            _options.ExchangeName,
            _options.RoutingKey);
    }

    private sealed record NotificationRecipient(string Id, string Email, string Name);
}
