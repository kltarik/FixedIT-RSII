using FixedIT.API.DTOs.Chat;
using FixedIT.API.DTOs.Common;

namespace FixedIT.API.Services;

public interface IConversationService
{
    Task<ConversationResponse> CreateAsync(
        string userId,
        CreateConversationRequest request,
        CancellationToken cancellationToken);

    Task<PagedResponse<ConversationResponse>> GetMineAsync(
        string userId,
        PagedRequest request,
        CancellationToken cancellationToken);

    Task<PagedResponse<MessageResponse>> GetMessagesAsync(
        string userId,
        int conversationId,
        PagedRequest request,
        CancellationToken cancellationToken);

    Task EnsureParticipantAsync(
        string userId,
        int conversationId,
        CancellationToken cancellationToken);

    Task<MessageResponse> CreateMessageAsync(
        string userId,
        int conversationId,
        string content,
        CancellationToken cancellationToken);
}
