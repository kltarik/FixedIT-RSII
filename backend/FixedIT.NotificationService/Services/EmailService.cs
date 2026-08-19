using System.Globalization;
using FixedIT.NotificationService.Configuration;
using FixedIT.Shared.Messages;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FixedIT.NotificationService.Services;

public sealed class EmailService(
    IOptions<SmtpOptions> options,
    ILogger<EmailService> logger) : IEmailService
{
    private readonly SmtpOptions _options = options.Value;

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
        message.MessageId = $"<{notification.MessageId:D}@fixedit.local>";

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
            _ => throw new InvalidOperationException(
                $"Tip poruke obavijesti {notification.GetType().Name} nije podržan.")
        };
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
