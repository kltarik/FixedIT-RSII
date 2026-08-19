using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FixedIT.API.Configuration;
using FixedIT.API.CustomExceptions;
using Microsoft.Extensions.Options;

namespace FixedIT.API.Services;

internal static class PayPalHttpClientNames
{
    public const string Api = "PayPal";
}

internal sealed record PayPalOrderResult(
    string OrderId,
    string Status,
    string? ApprovalUrl);

internal sealed record PayPalCaptureResult(
    string OrderId,
    string Status,
    string ReservationReference,
    string CaptureId,
    string CaptureStatus,
    decimal Amount,
    string Currency);

internal sealed record PayPalRefundResult(
    string RefundId,
    string Status,
    decimal Amount,
    string Currency);

internal interface IPayPalService
{
    Task<PayPalOrderResult> CreateOrderAsync(
        int reservationId,
        decimal amount,
        string currency,
        string requestId,
        CancellationToken cancellationToken);

    Task<PayPalOrderResult> GetOrderAsync(
        string orderId,
        CancellationToken cancellationToken);

    Task<PayPalCaptureResult> CaptureOrderAsync(
        string orderId,
        string requestId,
        CancellationToken cancellationToken);

    Task<PayPalRefundResult> RefundAsync(
        string captureId,
        decimal amount,
        string currency,
        string requestId,
        CancellationToken cancellationToken);

    Task<bool> VerifyWebhookSignatureAsync(
        PayPalWebhookHeaders headers,
        JsonElement webhookEvent,
        CancellationToken cancellationToken);
}

internal sealed class PayPalService(
    IHttpClientFactory httpClientFactory,
    IOptions<PayPalOptions> options,
    ILogger<PayPalService> logger) : IPayPalService, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private readonly PayPalOptions _options = options.Value;
    private string? _accessToken;
    private DateTime _accessTokenExpiresAtUtc;

    public async Task<PayPalOrderResult> CreateOrderAsync(
        int reservationId,
        decimal amount,
        string currency,
        string requestId,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    reference_id = reservationId.ToString(CultureInfo.InvariantCulture),
                    custom_id = reservationId.ToString(CultureInfo.InvariantCulture),
                    description = $"FixedIT reservation #{reservationId}",
                    amount = new
                    {
                        currency_code = currency,
                        value = FormatAmount(amount)
                    }
                }
            },
            payment_source = new
            {
                paypal = new
                {
                    experience_context = new
                    {
                        return_url = _options.ReturnUrl,
                        cancel_url = _options.CancelUrl,
                        user_action = "PAY_NOW",
                        shipping_preference = "NO_SHIPPING"
                    }
                }
            }
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, "v2/checkout/orders")
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };
        request.Headers.TryAddWithoutValidation("PayPal-Request-Id", requestId);
        using var response = await SendAuthorizedAsync(request, cancellationToken);
        using var document = await ReadSuccessJsonAsync(
            response,
            "create order",
            cancellationToken);
        return ParseProviderResponse(() => ParseOrder(document.RootElement));
    }

    public async Task<PayPalOrderResult> GetOrderAsync(
        string orderId,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"v2/checkout/orders/{Uri.EscapeDataString(orderId)}");
        using var response = await SendAuthorizedAsync(request, cancellationToken);
        using var document = await ReadSuccessJsonAsync(
            response,
            "get order",
            cancellationToken);
        return ParseProviderResponse(() => ParseOrder(document.RootElement));
    }

    public async Task<PayPalCaptureResult> CaptureOrderAsync(
        string orderId,
        string requestId,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"v2/checkout/orders/{Uri.EscapeDataString(orderId)}/capture")
        {
            Content = JsonContent.Create(new { }, options: SerializerOptions)
        };
        request.Headers.TryAddWithoutValidation("PayPal-Request-Id", requestId);
        using var response = await SendAuthorizedAsync(request, cancellationToken);
        using var document = await ReadSuccessJsonAsync(
            response,
            "capture order",
            cancellationToken);
        return ParseProviderResponse(() => ParseCapture(document.RootElement));
    }

    public async Task<PayPalRefundResult> RefundAsync(
        string captureId,
        decimal amount,
        string currency,
        string requestId,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            amount = new
            {
                currency_code = currency,
                value = FormatAmount(amount)
            }
        };
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"v2/payments/captures/{Uri.EscapeDataString(captureId)}/refund")
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };
        request.Headers.TryAddWithoutValidation("PayPal-Request-Id", requestId);
        using var response = await SendAuthorizedAsync(request, cancellationToken);
        using var document = await ReadSuccessJsonAsync(
            response,
            "refund capture",
            cancellationToken);
        return ParseProviderResponse(() => ParseRefund(document.RootElement));
    }

    public async Task<bool> VerifyWebhookSignatureAsync(
        PayPalWebhookHeaders headers,
        JsonElement webhookEvent,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            transmission_id = headers.TransmissionId,
            transmission_time = headers.TransmissionTime,
            cert_url = headers.CertificateUrl,
            auth_algo = headers.AuthenticationAlgorithm,
            transmission_sig = headers.TransmissionSignature,
            webhook_id = _options.WebhookId,
            webhook_event = webhookEvent
        };
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "v1/notifications/verify-webhook-signature")
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };
        using var response = await SendAuthorizedAsync(request, cancellationToken);
        using var document = await ReadSuccessJsonAsync(
            response,
            "verify webhook signature",
            cancellationToken);
        return document.RootElement.TryGetProperty("verification_status", out var status)
            && string.Equals(status.GetString(), "SUCCESS", StringComparison.Ordinal);
    }

    public void Dispose()
    {
        _tokenLock.Dispose();
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await GetAccessTokenAsync(cancellationToken));
        var client = httpClientFactory.CreateClient(PayPalHttpClientNames.Api);
        return await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_accessToken is not null && _accessTokenExpiresAtUtc > DateTime.UtcNow)
        {
            return _accessToken;
        }

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (_accessToken is not null && _accessTokenExpiresAtUtc > DateTime.UtcNow)
            {
                return _accessToken;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "v1/oauth2/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials"
                })
            };
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var client = httpClientFactory.CreateClient(PayPalHttpClientNames.Api);
            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            using var document = await ReadSuccessJsonAsync(
                response,
                "obtain access token",
                cancellationToken);
            var tokenResponse = document.RootElement.Deserialize<AccessTokenResponse>(SerializerOptions)
                ?? throw new BusinessException("PayPal je vratio neispravan odgovor za pristupni token.");
            if (string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
            {
                throw new BusinessException("PayPal je vratio neispravan odgovor za pristupni token.");
            }

            _accessToken = tokenResponse.AccessToken;
            _accessTokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(
                Math.Max(1, tokenResponse.ExpiresIn - 60));
            return _accessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<JsonDocument> ReadSuccessJsonAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var debugId = response.Headers.TryGetValues("PayPal-Debug-Id", out var values)
                ? values.FirstOrDefault()
                : null;
            logger.LogWarning(
                "PayPal {Operation} failed with status {StatusCode} and debug ID {DebugId}.",
                operation,
                (int)response.StatusCode,
                debugId ?? "unavailable");
            throw new BusinessException("Pružalac usluge plaćanja nije mogao završiti zahtjev.");
        }

        try
        {
            return JsonDocument.Parse(rawBody);
        }
        catch (JsonException exception)
        {
            logger.LogError(exception, "PayPal {Operation} returned invalid JSON.", operation);
            throw new BusinessException("Pružalac usluge plaćanja vratio je neispravan odgovor.");
        }
    }

    private static PayPalOrderResult ParseOrder(JsonElement root)
    {
        var orderId = GetRequiredString(root, "id");
        var status = GetRequiredString(root, "status");
        string? approvalUrl = null;
        if (root.TryGetProperty("links", out var links))
        {
            foreach (var link in links.EnumerateArray())
            {
                if (link.TryGetProperty("rel", out var relation)
                    && (string.Equals(
                        relation.GetString(),
                        "payer-action",
                        StringComparison.Ordinal)
                    || string.Equals(
                        relation.GetString(),
                        "approve",
                        StringComparison.Ordinal)))
                {
                    approvalUrl = GetRequiredString(link, "href");
                    break;
                }
            }
        }

        return new PayPalOrderResult(orderId, status, approvalUrl);
    }

    private static PayPalCaptureResult ParseCapture(JsonElement root)
    {
        var purchaseUnit = root.GetProperty("purchase_units")[0];
        var capture = purchaseUnit.GetProperty("payments").GetProperty("captures")[0];
        var amount = capture.GetProperty("amount");
        return new PayPalCaptureResult(
            GetRequiredString(root, "id"),
            GetRequiredString(root, "status"),
            GetRequiredString(purchaseUnit, "reference_id"),
            GetRequiredString(capture, "id"),
            GetRequiredString(capture, "status"),
            ParseAmount(amount),
            GetRequiredString(amount, "currency_code"));
    }

    private static PayPalRefundResult ParseRefund(JsonElement root)
    {
        var amount = root.GetProperty("amount");
        return new PayPalRefundResult(
            GetRequiredString(root, "id"),
            GetRequiredString(root, "status"),
            ParseAmount(amount),
            GetRequiredString(amount, "currency_code"));
    }

    private static decimal ParseAmount(JsonElement amount)
    {
        var value = GetRequiredString(amount, "value");
        if (!decimal.TryParse(
            value,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var parsed))
        {
            throw new BusinessException("PayPal je vratio neispravan iznos plaćanja.");
        }

        return parsed;
    }

    private static string GetRequiredString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property)
            || string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new BusinessException("PayPal je vratio nepotpun odgovor.");
        }

        return property.GetString()!;
    }

    private static string FormatAmount(decimal amount)
    {
        return amount.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private static T ParseProviderResponse<T>(Func<T> parser)
    {
        try
        {
            return parser();
        }
        catch (Exception exception) when (exception is KeyNotFoundException
            or InvalidOperationException
            or IndexOutOfRangeException
            or JsonException)
        {
            throw new BusinessException("Pružalac usluge plaćanja vratio je nepotpun odgovor.");
        }
    }

    private sealed class AccessTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
