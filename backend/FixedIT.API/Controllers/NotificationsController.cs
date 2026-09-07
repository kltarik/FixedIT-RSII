using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Notifications;
using FixedIT.API.Extensions;
using FixedIT.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixedIT.API.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(INotificationService notificationService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<NotificationResponse>>> GetPage(
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await notificationService.GetPageAsync(
            User.GetUserId(),
            request,
            cancellationToken));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount(CancellationToken cancellationToken)
    {
        return Ok(await notificationService.GetUnreadCountAsync(
            User.GetUserId(),
            cancellationToken));
    }

    [HttpPut("{id:int}/read")]
    public async Task<ActionResult<NotificationResponse>> MarkRead(
        int id,
        CancellationToken cancellationToken)
    {
        return Ok(await notificationService.MarkReadAsync(
            User.GetUserId(),
            id,
            cancellationToken));
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await notificationService.MarkAllReadAsync(
            User.GetUserId(),
            cancellationToken);
        return NoContent();
    }
}
