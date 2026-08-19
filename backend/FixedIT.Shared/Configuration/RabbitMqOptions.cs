using System.ComponentModel.DataAnnotations;

namespace FixedIT.Shared.Configuration;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    [Required]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; }

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string VirtualHost { get; set; } = string.Empty;

    [Required]
    public string ExchangeName { get; set; } = string.Empty;

    [Required]
    public string QueueName { get; set; } = string.Empty;

    [Required]
    public string RoutingKey { get; set; } = string.Empty;

    [Required]
    public string DeadLetterExchangeName { get; set; } = string.Empty;

    [Required]
    public string DeadLetterQueueName { get; set; } = string.Empty;

    [Required]
    public string DeadLetterRoutingKey { get; set; } = string.Empty;

    [Range(1, 300)]
    public int ConnectionRetrySeconds { get; set; }

    [Range(1, ushort.MaxValue)]
    public ushort PrefetchCount { get; set; }

    [Range(1, 120)]
    public int PublishConfirmTimeoutSeconds { get; set; }

    [Range(100, 1_000_000)]
    public int ProcessedMessageCacheSize { get; set; }
}
