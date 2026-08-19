using FixedIT.NotificationService.Configuration;
using FixedIT.NotificationService.Consumers;
using FixedIT.NotificationService.Services;
using FixedIT.Shared.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services
    .AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services
    .AddOptions<SmtpOptions>()
    .Bind(builder.Configuration.GetSection(SmtpOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => string.IsNullOrWhiteSpace(options.Username)
            || !string.IsNullOrWhiteSpace(options.Password),
        "Smtp:Password je obavezan kada je Smtp:Username konfigurisan.")
    .ValidateOnStart();
builder.Services.AddSingleton<IConnectionFactory>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
    return new ConnectionFactory
    {
        HostName = options.Host,
        Port = options.Port,
        UserName = options.Username,
        Password = options.Password,
        VirtualHost = options.VirtualHost,
        AutomaticRecoveryEnabled = false,
        TopologyRecoveryEnabled = false,
        DispatchConsumersAsync = true
    };
});
builder.Services.AddSingleton<ProcessedMessageCache>();
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddHostedService<NotificationConsumer>();

var host = builder.Build();
host.Run();
