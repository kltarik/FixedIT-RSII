using FixedIT.API.DTOs.Chat;
using FixedIT.API.DTOs.Common;
using FixedIT.API.Extensions;
using FixedIT.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixedIT.API.Controllers;

[ApiController]
[Authorize]
[Route("api/conversations")]
public sealed class ConversationsController(IConversationService conversationService)
    : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ConversationResponse>> Create(
        CreateConversationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await conversationService.CreateAsync(
            User.GetUserId(),
            request,
            cancellationToken);
        return CreatedAtAction(
            nameof(GetMessages),
            new { id = response.Id, page = 1 },
            response);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<ConversationResponse>>> GetMine(
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await conversationService.GetMineAsync(
            User.GetUserId(),
            request,
            cancellationToken));
    }

    [HttpGet("{id:int}/messages")]
    public async Task<ActionResult<PagedResponse<MessageResponse>>> GetMessages(
        int id,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await conversationService.GetMessagesAsync(
            User.GetUserId(),
            id,
            request,
            cancellationToken));
    }
}
