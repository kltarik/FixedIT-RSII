using FixedIT.API.Constants;
using FixedIT.API.CustomExceptions;
using FixedIT.API.DTOs.Payments;
using FixedIT.API.Extensions;
using FixedIT.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixedIT.API.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    [Authorize(Roles = RoleNames.Client)]
    [HttpPost("create-order")]
    public async Task<ActionResult<PaymentOrderResponse>> CreateOrder(
        CreatePaymentOrderRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await paymentService.CreateOrderAsync(
            User.GetUserId(),
            request.ReservationId,
            cancellationToken));
    }

    [Authorize(Roles = RoleNames.Client)]
    [HttpPost("capture/{orderId}")]
    public async Task<ActionResult<PaymentResponse>> Capture(
        string orderId,
        CancellationToken cancellationToken)
    {
        return Ok(await paymentService.CaptureOrderAsync(
            User.GetUserId(),
            orderId,
            cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        var headers = GetWebhookHeaders();
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return BadRequest();
        }

        return await paymentService.HandleWebhookAsync(
            headers,
            rawBody,
            cancellationToken)
            ? Ok()
            : Unauthorized();
    }

    [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
    [HttpPost("refund/{paymentId:int}")]
    public async Task<ActionResult<PaymentResponse>> Refund(
        int paymentId,
        CancellationToken cancellationToken)
    {
        return Ok(await paymentService.RefundByIdAsync(
            User.GetUserId(),
            paymentId,
            cancellationToken));
    }

    private PayPalWebhookHeaders GetWebhookHeaders()
    {
        return new PayPalWebhookHeaders(
            GetRequiredHeader("PayPal-Transmission-Id"),
            GetRequiredHeader("PayPal-Transmission-Time"),
            GetRequiredHeader("PayPal-Cert-Url"),
            GetRequiredHeader("PayPal-Auth-Algo"),
            GetRequiredHeader("PayPal-Transmission-Sig"));
    }

    private string GetRequiredHeader(string name)
    {
        var value = Request.Headers[name].ToString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessException($"Nedostaje obavezno PayPal zaglavlje {name}.");
        }

        return value;
    }
}
