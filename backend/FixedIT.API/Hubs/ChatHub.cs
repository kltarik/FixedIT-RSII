using FixedIT.API.CustomExceptions;
using FixedIT.API.DTOs.Chat;
using FixedIT.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FixedIT.API.Hubs;

[Authorize]
public sealed class ChatHub(IConversationService conversationService) : Hub<IChatClient>
{
    public async Task JoinConversation(int conversationId)
    {
        var userId = GetUserId();
        try
        {
            await conversationService.EnsureParticipantAsync(
                userId,
                conversationId,
                Context.ConnectionAborted);
        }
        catch (NotFoundException)
        {
            throw new HubException("Niste ovlašteni za ovu radnju.");
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GetGroupName(conversationId),
            Context.ConnectionAborted);
    }

    public async Task<MessageResponse> SendMessage(int conversationId, string content)
    {
        var userId = GetUserId();
        MessageResponse message;
        try
        {
            message = await conversationService.CreateMessageAsync(
                userId,
                conversationId,
                content,
                Context.ConnectionAborted);
        }
        catch (NotFoundException)
        {
            throw new HubException("Niste ovlašteni za ovu radnju.");
        }
        catch (BusinessException exception)
        {
            throw new HubException(exception.Message);
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GetGroupName(conversationId),
            Context.ConnectionAborted);
        await Clients.Group(GetGroupName(conversationId)).ReceiveMessage(message);
        return message;
    }

    private string GetUserId()
    {
        return Context.UserIdentifier
            ?? throw new HubException("Niste ovlašteni za ovu radnju.");
    }

    private static string GetGroupName(int conversationId)
    {
        return $"conversation-{conversationId}";
    }
}
