using System.Globalization;
using System.Text.Json;
using FixedIT.NotificationService.Configuration;
using FixedIT.Shared.Messages;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FixedIT.NotificationService.Services;

public sealed class EmailService(
    IHttpClientFactory httpClientFactory,
    IOptions<SmtpOptions> options,
    ILogger<EmailService> logger) : IEmailService
{
    private static readonly TimeZoneInfo BosniaTimeZone = ResolveBosniaTimeZone();
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly SmtpOptions _options = options.Value;

    public async Task<bool> WasDeliveredAsync(
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var expectedMessageId = BuildMessageId(messageId);
        var client = httpClientFactory.CreateClient(SmtpHttpClientNames.DeliveryStatus);
        const int pageSize = 250;
        var start = 0;

        while (true)
        {
            using var response = await client.GetAsync(
                $"api/v2/messages?start={start}&limit={pageSize}",
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            response.EnsureSuccessStatusCode();
            await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            var page = await JsonSerializer.DeserializeAsync<MailHogMessagesResponse>(
                content,
                SerializerOptions,
                cancellationToken);

            if (page?.Items.Any(message => message.HasMessageId(expectedMessageId)) == true)
            {
                return true;
            }

            var count = page?.Items.Count ?? 0;
            start += count;
            if (count == 0 || start >= page!.Total)
            {
                return false;
            }
        }
    }

    public async Task SendAsync(
        BaseNotificationMessage notification,
        CancellationToken cancellationToken)
    {
        var (subject, body) = Format(notification);
        var message = new MimeMessage
        {
            Subject = subject,
            Body = new TextPart("plain") { Text = body }
        };
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(new MailboxAddress(
            notification.RecipientName,
            notification.RecipientEmail));
        message.MessageId = BuildMessageId(notification.MessageId);

        using var client = new SmtpClient();
        var socketOptions = _options.UseSsl
            ? SecureSocketOptions.Auto
            : SecureSocketOptions.None;
        await client.ConnectAsync(
            _options.Host,
            _options.Port,
            socketOptions,
            cancellationToken);
        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            await client.AuthenticateAsync(
                _options.Username,
                _options.Password,
                cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
        logger.LogInformation(
            "Sent {MessageType} email for notification {MessageId}.",
            notification.GetType().Name,
            notification.MessageId);
    }

    private static string BuildMessageId(Guid messageId) => $"<{messageId:D}@fixedit.local>";

    private sealed class MailHogMessagesResponse
    {
        public int Total { get; init; }

        public List<MailHogMessage> Items { get; init; } = [];
    }

    private sealed class MailHogMessage
    {
        public MailHogContent Content { get; init; } = new();

        public bool HasMessageId(string expectedMessageId)
        {
            return Content.Headers.Any(header =>
                string.Equals(header.Key, "Message-ID", StringComparison.OrdinalIgnoreCase)
                && header.Value.Any(value => string.Equals(
                    value,
                    expectedMessageId,
                    StringComparison.OrdinalIgnoreCase)));
        }
    }

    private sealed class MailHogContent
    {
        public Dictionary<string, string[]> Headers { get; init; } = [];
    }

    private static (string Subject, string Body) Format(BaseNotificationMessage notification)
    {
        return notification switch
        {
            ReservationStatusChangedMessage reservation => (
                $"Promijenjen status rezervacije #{reservation.ReservationId}",
                BuildReservationBody(reservation)),
            NewMessageNotificationMessage chat => (
                $"Nova poruka od korisnika {chat.SenderName}",
                $"Primili ste novu poruku u razgovoru #{chat.ConversationId}:{Environment.NewLine}{Environment.NewLine}{chat.ContentPreview}"),
            PaymentCompletedMessage payment => (
                $"Završeno plaćanje za rezervaciju #{payment.ReservationId}",
                $"Plaćanje u iznosu od {payment.Amount.ToString("0.00", CultureInfo.InvariantCulture)} {payment.Currency} uspješno je završeno."),
            PasswordResetRequestedMessage passwordReset => (
                "Kod za promjenu FixedIT lozinke",
                $"Vaš jednokratni kod je: {passwordReset.Code}{Environment.NewLine}{Environment.NewLine}Kod vrijedi do {FormatBosniaTime(passwordReset.ExpiresAt)} po vremenu u BiH."),
            _ => throw new InvalidOperationException(
                $"Tip poruke obavijesti {notification.GetType().Name} nije podržan.")
        };
    }

    private static string FormatBosniaTime(DateTime value)
    {
        var utcValue = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
        return TimeZoneInfo.ConvertTimeFromUtc(utcValue, BosniaTimeZone)
            .ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
    }

    private static TimeZoneInfo ResolveBosniaTimeZone()
    {
        foreach (var timeZoneId in new[]
                 {
                     "Europe/Sarajevo",
                     "Central European Standard Time"
                 })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                // Try the platform-specific identifier below.
            }
        }

        throw new InvalidOperationException("Vremenska zona za Bosnu i Hercegovinu nije dostupna.");
    }

    private static string BuildReservationBody(ReservationStatusChangedMessage message)
    {
        var transition = string.IsNullOrWhiteSpace(message.PreviousStatus)
            ? LocalizeStatus(message.Status)
            : $"{LocalizeStatus(message.PreviousStatus)} -> {LocalizeStatus(message.Status)}";
        var body = $"Status rezervacije #{message.ReservationId}: {transition}.";
        return string.IsNullOrWhiteSpace(message.Reason)
            ? body
            : $"{body}{Environment.NewLine}Razlog: {message.Reason}";
    }

    private static string LocalizeStatus(string status) => status switch
    {
        "Pending" => "na čekanju",
        "Accepted" => "prihvaćena",
        "InProgress" => "u toku",
        "Completed" => "završena",
        "Cancelled" => "otkazana",
        _ => "nepoznat"
    };
}
