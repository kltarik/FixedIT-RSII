using System.Text.Json;
using FixedIT.NotificationService.Services;
using FixedIT.Shared.Configuration;
using FixedIT.Shared.Messages;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FixedIT.NotificationService.Consumers;

public sealed class NotificationConsumer(
    IConnectionFactory connectionFactory,
    IEmailService emailService,
    ProcessedMessageCache processedMessages,
    IOptions<RabbitMqOptions> options,
    ILogger<NotificationConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeUntilDisconnectedAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "RabbitMQ consumer stopped unexpectedly. Reconnecting in {RetrySeconds} seconds.",
                    _options.ConnectionRetrySeconds);
                await Task.Delay(
                    TimeSpan.FromSeconds(_options.ConnectionRetrySeconds),
                    stoppingToken);
            }
        }
    }

    private async Task ConsumeUntilDisconnectedAsync(CancellationToken stoppingToken)
    {
        using var connection = connectionFactory.CreateConnection("FixedIT.NotificationService");
        using var channel = connection.CreateModel();
        DeclareTopology(channel);
        channel.BasicQos(
            prefetchSize: 0,
            prefetchCount: _options.PrefetchCount,
            global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, eventArgs) =>
            await HandleMessageAsync(channel, eventArgs, stoppingToken);
        channel.BasicConsume(
            _options.QueueName,
            autoAck: false,
            consumer);
        logger.LogInformation(
            "Notification consumer is listening on RabbitMQ queue {QueueName}.",
            _options.QueueName);

        var disconnected = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        connection.ConnectionShutdown += (_, eventArgs) =>
        {
            logger.LogWarning(
                "RabbitMQ connection shut down: {ReplyCode} {ReplyText}.",
                eventArgs.ReplyCode,
                eventArgs.ReplyText);
            disconnected.TrySetResult();
        };
        using var cancellationRegistration = stoppingToken.Register(
            () => disconnected.TrySetCanceled(stoppingToken));
        await disconnected.Task;
    }

    private async Task HandleMessageAsync(
        IModel channel,
        BasicDeliverEventArgs eventArgs,
        CancellationToken stoppingToken)
    {
        Guid? messageId = null;
        try
        {
            var notification = JsonSerializer.Deserialize<BaseNotificationMessage>(
                eventArgs.Body.Span,
                SerializerOptions)
                ?? throw new JsonException("Sadržaj poruke obavijesti je prazan.");
            if (notification.MessageId == Guid.Empty)
            {
                throw new JsonException("Nedostaje identifikator poruke obavijesti.");
            }

            messageId = notification.MessageId;
            if (processedMessages.Contains(notification.MessageId))
            {
                logger.LogWarning(
                    "Skipping duplicate notification message {MessageId}.",
                    notification.MessageId);
                channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                return;
            }

            await emailService.SendAsync(notification, stoppingToken);
            processedMessages.Add(notification.MessageId);
            channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            logger.LogInformation(
                "Acknowledged notification message {MessageId} with delivery tag {DeliveryTag}.",
                notification.MessageId,
                eventArgs.DeliveryTag);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            if (channel.IsOpen)
            {
                channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Error processing notification message {MessageId} with delivery tag {DeliveryTag}.",
                messageId,
                eventArgs.DeliveryTag);
            if (channel.IsOpen)
            {
                channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
            }
        }
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
}
