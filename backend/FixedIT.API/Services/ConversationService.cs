using System.Data;
using System.Linq.Expressions;
using FixedIT.API.Constants;
using FixedIT.API.CustomExceptions;
using FixedIT.API.Data;
using FixedIT.API.DTOs.Chat;
using FixedIT.API.DTOs.Common;
using FixedIT.API.Models;
using FixedIT.API.Models.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FixedIT.API.Services;

public sealed class ConversationService(
    AppDbContext db,
    IPaginationService paginationService,
    INotificationService notificationService,
    INotificationEventPublisher eventPublisher) : IConversationService
{
    private static readonly Expression<Func<Message, MessageResponse>> MessageProjection =
        message => new MessageResponse(
            message.Id,
            message.ConversationId,
            message.SenderUserId,
            message.SenderUser.FirstName,
            message.SenderUser.LastName,
            message.Content,
            message.SentAt);

    public async Task<ConversationResponse> CreateAsync(
        string userId,
        CreateConversationRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        int? existingConversationId;
        Conversation conversation;
        if (request.ReservationId.HasValue)
        {
            var reservation = await db.Reservations
                .Where(item => item.Id == request.ReservationId.Value
                    && (item.ClientUserId == userId
                        || item.ProfessionalProfile.UserId == userId))
                .Select(item => new
                {
                    item.Id,
                    item.ClientUserId,
                    ProfessionalUserId = item.ProfessionalProfile.UserId
                })
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Rezervacija nije pronađena.");

            existingConversationId = await db.Conversations
                .Where(item => item.ReservationId == reservation.Id)
                .Select(item => (int?)item.Id)
                .SingleOrDefaultAsync(cancellationToken);
            if (existingConversationId.HasValue)
            {
                await transaction.CommitAsync(cancellationToken);
                return await GetByIdAsync(userId, existingConversationId.Value, cancellationToken);
            }

            conversation = CreateConversation(
                reservation.Id,
                reservation.ClientUserId,
                reservation.ProfessionalUserId);
        }
        else
        {
            var participantUserId = request.ParticipantUserId?.Trim();
            if (string.IsNullOrWhiteSpace(participantUserId))
            {
                throw new BusinessException("Potrebno je navesti učesnika razgovora.");
            }

            if (string.Equals(userId, participantUserId, StringComparison.Ordinal))
            {
                throw new BusinessException("Direktan razgovor zahtijeva drugog učesnika.");
            }

            if (!await db.Users.AnyAsync(
                    user => user.Id == participantUserId && user.IsActive,
                    cancellationToken))
            {
                throw new NotFoundException("Učesnik razgovora nije pronađen.");
            }

            existingConversationId = await db.Conversations
                .Where(item => item.ReservationId == null
                    && item.Participants.Count == 2
                    && item.Participants.Any(participant => participant.UserId == userId)
                    && item.Participants.Any(participant => participant.UserId == participantUserId))
                .OrderBy(item => item.Id)
                .Select(item => (int?)item.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (existingConversationId.HasValue)
            {
                await transaction.CommitAsync(cancellationToken);
                return await GetByIdAsync(userId, existingConversationId.Value, cancellationToken);
            }

            conversation = CreateConversation(null, userId, participantUserId);
        }

        db.Conversations.Add(conversation);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            request.ReservationId.HasValue && IsUniqueConstraintViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            await transaction.DisposeAsync();
            db.ChangeTracker.Clear();
            var concurrentConversationId = await db.Conversations
                .AsNoTracking()
                .Where(item => item.ReservationId == request.ReservationId.Value)
                .Select(item => (int?)item.Id)
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new BusinessException("Razgovor za rezervaciju nije moguće kreirati.");
            return await GetByIdAsync(userId, concurrentConversationId, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return await GetByIdAsync(userId, conversation.Id, cancellationToken);
    }

    public async Task<PagedResponse<ConversationResponse>> GetMineAsync(
        string userId,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        var page = paginationService.Normalize(request);
        var query = db.Conversations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(conversation => conversation.Participants
                .Any(participant => participant.UserId == userId));
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(conversation => conversation.Messages
                .Select(message => (DateTime?)message.SentAt)
                .Max() ?? conversation.CreatedAt)
            .ThenByDescending(conversation => conversation.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(conversation => new ConversationResponse(
                conversation.Id,
                conversation.ReservationId,
                conversation.CreatedAt,
                conversation.Participants
                    .OrderBy(participant => participant.UserId)
                    .Select(participant => new ConversationParticipantResponse(
                        participant.UserId,
                        participant.User.FirstName,
                        participant.User.LastName,
                        participant.User.ProfilePictureUrl))
                    .ToList(),
                conversation.Messages
                    .OrderByDescending(message => message.SentAt)
                    .ThenByDescending(message => message.Id)
                    .Select(message => new MessageResponse(
                        message.Id,
                        message.ConversationId,
                        message.SenderUserId,
                        message.SenderUser.FirstName,
                        message.SenderUser.LastName,
                        message.Content,
                        message.SentAt))
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new PagedResponse<ConversationResponse>(
            items,
            total,
            page.Page,
            page.PageSize);
    }

    public async Task<PagedResponse<MessageResponse>> GetMessagesAsync(
        string userId,
        int conversationId,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureParticipantAsync(userId, conversationId, cancellationToken);
        var page = paginationService.Normalize(request);
        var query = db.Messages
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(message => message.SentAt)
            .ThenByDescending(message => message.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(MessageProjection)
            .ToListAsync(cancellationToken);

        return new PagedResponse<MessageResponse>(
            items,
            total,
            page.Page,
            page.PageSize);
    }

    public async Task EnsureParticipantAsync(
        string userId,
        int conversationId,
        CancellationToken cancellationToken)
    {
        if (!await db.ConversationParticipants.IgnoreQueryFilters().AnyAsync(
                participant => participant.ConversationId == conversationId
                    && participant.UserId == userId,
                cancellationToken))
        {
            throw new NotFoundException("Razgovor nije pronađen.");
        }
    }

    public async Task<MessageResponse> CreateMessageAsync(
        string userId,
        int conversationId,
        string content,
        CancellationToken cancellationToken)
    {
        var sender = await db.ConversationParticipants
            .Where(participant => participant.ConversationId == conversationId
                && participant.UserId == userId)
            .Select(participant => new
            {
                participant.User.FirstName,
                participant.User.LastName
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Razgovor nije pronađen.");

        var normalizedContent = content?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedContent))
        {
            throw new BusinessException("Sadržaj poruke je obavezan.");
        }

        if (normalizedContent.Length > DatabaseConstants.ContentMaxLength)
        {
            throw new BusinessException(
                $"Sadržaj poruke ne može biti duži od {DatabaseConstants.ContentMaxLength} znakova.");
        }

        var message = new Message
        {
            ConversationId = conversationId,
            SenderUserId = userId,
            Content = normalizedContent,
            SentAt = DateTime.UtcNow
        };
        db.Messages.Add(message);
        await db.SaveChangesAsync(cancellationToken);

        var senderName = $"{sender.FirstName} {sender.LastName}";
        var recipientIds = await db.ConversationParticipants
            .Where(participant => participant.ConversationId == conversationId
                && participant.UserId != userId)
            .Select(participant => participant.UserId)
            .ToListAsync(cancellationToken);
        var preview = message.Content.Length <= 200
            ? message.Content
            : message.Content[..200];
        await notificationService.CreateAndPushAsync(
            recipientIds.Select(recipientId => new CreateNotificationCommand(
                recipientId,
                $"Nova poruka od {senderName}",
                preview,
                NotificationType.Message,
                message.SentAt)).ToArray(),
            cancellationToken);
        await eventPublisher.PublishNewMessageAsync(
            new NewMessageNotificationEvent(
                conversationId,
                message.Id,
                userId,
                senderName,
                message.Content,
                message.SentAt),
            cancellationToken);

        return new MessageResponse(
            message.Id,
            message.ConversationId,
            message.SenderUserId,
            sender.FirstName,
            sender.LastName,
            message.Content,
            message.SentAt);
    }

    private async Task<ConversationResponse> GetByIdAsync(
        string userId,
        int conversationId,
        CancellationToken cancellationToken)
    {
        return await db.Conversations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(conversation => conversation.Id == conversationId
                && conversation.Participants.Any(participant => participant.UserId == userId))
            .Select(conversation => new ConversationResponse(
                conversation.Id,
                conversation.ReservationId,
                conversation.CreatedAt,
                conversation.Participants
                    .OrderBy(participant => participant.UserId)
                    .Select(participant => new ConversationParticipantResponse(
                        participant.UserId,
                        participant.User.FirstName,
                        participant.User.LastName,
                        participant.User.ProfilePictureUrl))
                    .ToList(),
                null))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Razgovor nije pronađen.");
    }

    private static Conversation CreateConversation(
        int? reservationId,
        string firstParticipantUserId,
        string secondParticipantUserId)
    {
        return new Conversation
        {
            ReservationId = reservationId,
            CreatedAt = DateTime.UtcNow,
            Participants =
            [
                new ConversationParticipant { UserId = firstParticipantUserId },
                new ConversationParticipant { UserId = secondParticipantUserId }
            ]
        };
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException { Number: 2601 or 2627 };
    }
}
