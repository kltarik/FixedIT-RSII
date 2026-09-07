using System.Text;
using FixedIT.API.Data;
using FixedIT.Shared.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace FixedIT.API.BackgroundServices;

public sealed class OutboxPublisherService(
    IServiceScopeFactory scopeFactory,
    IConnection connection,
    IOptions<RabbitMqOptions> options,
    ILogger<OutboxPublisherService> logger) : BackgroundService
{
    private readonly RabbitMqOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Objavljivanje RabbitMQ outbox poruka nije uspjelo.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.OutboxPollingSeconds), stoppingToken);
        }
    }

    private async Task PublishBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var messages = await db.OutboxMessages
            .Where(message => message.PublishedAt == null
                && (message.NextAttemptAt == null || message.NextAttemptAt <= now))
            .OrderBy(message => message.CreatedAt)
            .Take(_options.OutboxBatchSize)
            .ToListAsync(cancellationToken);
        if (messages.Count == 0)
        {
            return;
        }

        using var channel = connection.CreateModel();
        DeclareTopology(channel);
        channel.ConfirmSelect();

        foreach (var message in messages)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = "application/json";
                properties.MessageId = message.Id.ToString();
                properties.Type = message.MessageType;
                channel.BasicPublish(
                    _options.ExchangeName,
                    _options.RoutingKey,
                    mandatory: true,
                    basicProperties: properties,
                    body: Encoding.UTF8.GetBytes(message.Payload));
                if (!channel.WaitForConfirms(TimeSpan.FromSeconds(_options.PublishConfirmTimeoutSeconds)))
                {
                    throw new InvalidOperationException("RabbitMQ nije potvrdio objavljivanje poruke.");
                }

                message.PublishedAt = DateTime.UtcNow;
                message.NextAttemptAt = null;
                message.LastError = null;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                message.AttemptCount++;
                var delaySeconds = Math.Min(300, (int)Math.Pow(2, Math.Min(message.AttemptCount, 8)));
                message.NextAttemptAt = DateTime.UtcNow.AddSeconds(delaySeconds);
                message.LastError = exception.Message.Length <= 2000
                    ? exception.Message
                    : exception.Message[..2000];
                logger.LogWarning(
                    exception,
                    "Outbox poruka {MessageId} nije objavljena; sljedeći pokušaj je za {DelaySeconds} sekundi.",
                    message.Id,
                    delaySeconds);
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private void DeclareTopology(IModel channel)
    {
        channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Direct, durable: true, autoDelete: false);
        channel.ExchangeDeclare(_options.DeadLetterExchangeName, ExchangeType.Direct, durable: true, autoDelete: false);
        channel.QueueDeclare(_options.DeadLetterQueueName, durable: true, exclusive: false, autoDelete: false);
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
        channel.QueueBind(_options.QueueName, _options.ExchangeName, _options.RoutingKey);
    }
}
