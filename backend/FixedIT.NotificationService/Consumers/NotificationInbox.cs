using FixedIT.NotificationService.Data;
using FixedIT.Shared.Messages;
using Microsoft.EntityFrameworkCore;

namespace FixedIT.NotificationService.Consumers;

public enum InboxClaimResult
{
    Acquired,
    Completed,
    InProgress
}

public sealed class NotificationInbox(NotificationDbContext db)
{
    private static readonly TimeSpan ProcessingLease = TimeSpan.FromMinutes(5);

    public async Task<InboxClaimResult> TryClaimAsync(
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var existing = await db.ProcessedNotificationMessages
            .SingleOrDefaultAsync(message => message.MessageId == messageId, cancellationToken);
        if (existing is not null)
        {
            if (existing.ProcessedAt.HasValue)
            {
                return InboxClaimResult.Completed;
            }

            if (existing.ReceivedAt > now.Subtract(ProcessingLease))
            {
                return InboxClaimResult.InProgress;
            }

            existing.ReceivedAt = now;
            await db.SaveChangesAsync(cancellationToken);
            return InboxClaimResult.Acquired;
        }

        db.ProcessedNotificationMessages.Add(new ProcessedNotificationMessage
        {
            MessageId = messageId,
            ReceivedAt = now
        });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return InboxClaimResult.Acquired;
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            existing = await db.ProcessedNotificationMessages
                .AsNoTracking()
                .SingleAsync(message => message.MessageId == messageId, cancellationToken);
            return existing.ProcessedAt.HasValue
                ? InboxClaimResult.Completed
                : InboxClaimResult.InProgress;
        }
    }

    public async Task CompleteAsync(Guid messageId, CancellationToken cancellationToken)
    {
        var message = await db.ProcessedNotificationMessages
            .SingleAsync(item => item.MessageId == messageId, cancellationToken);
        message.ProcessedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task ReleaseAsync(Guid messageId, CancellationToken cancellationToken)
    {
        return db.ProcessedNotificationMessages
            .Where(message => message.MessageId == messageId && message.ProcessedAt == null)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
