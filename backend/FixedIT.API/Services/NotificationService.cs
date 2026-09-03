using System.Linq.Expressions;
using FixedIT.API.CustomExceptions;
using FixedIT.API.Data;
using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Notifications;
using FixedIT.API.Hubs;
using FixedIT.API.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FixedIT.API.Services;

public sealed class NotificationService(
    AppDbContext db,
    IPaginationService paginationService,
    IHubContext<NotificationsHub, INotificationsClient> hubContext) : INotificationService
{
    private static readonly Expression<Func<Notification, NotificationResponse>> Projection =
        notification => new NotificationResponse(
            notification.Id,
            notification.Title,
            notification.Body,
            notification.IsRead,
            notification.CreatedAt,
            notification.Type);

    public async Task<PagedResponse<NotificationResponse>> GetPageAsync(
        string userId,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        var page = paginationService.Normalize(request);
        var query = db.Notifications
            .AsNoTracking()
            .Where(notification => notification.UserId == userId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(Projection)
            .ToListAsync(cancellationToken);
        return new PagedResponse<NotificationResponse>(
            items,
            total,
            page.Page,
            page.PageSize);
    }

    public async Task<NotificationResponse> MarkReadAsync(
        string userId,
        int notificationId,
        CancellationToken cancellationToken)
    {
        var notification = await db.Notifications
            .SingleOrDefaultAsync(
                item => item.Id == notificationId && item.UserId == userId,
                cancellationToken)
            ?? throw new NotFoundException("Obavijest nije pronađena.");
        notification.IsRead = true;
        await db.SaveChangesAsync(cancellationToken);
        return Map(notification);
    }

    public async Task MarkAllReadAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        await db.Notifications
            .Where(notification => notification.UserId == userId && !notification.IsRead)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(notification => notification.IsRead, true),
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<Notification>> CreateAndPushAsync(
        IReadOnlyCollection<CreateNotificationCommand> commands,
        CancellationToken cancellationToken)
    {
        if (commands.Count == 0)
        {
            return [];
        }

        var notifications = commands.Select(command => new Notification
        {
            UserId = command.UserId,
            Title = command.Title,
            Body = command.Body,
            IsRead = false,
            CreatedAt = command.CreatedAt,
            Type = command.Type
        }).ToArray();
        db.Notifications.AddRange(notifications);
        await db.SaveChangesAsync(cancellationToken);
        foreach (var notification in notifications)
        {
            await PushAsync(notification, cancellationToken);
        }

        return notifications;
    }

    public Task PushAsync(
        Notification notification,
        CancellationToken cancellationToken)
    {
        return hubContext.Clients.User(notification.UserId).ReceiveNotification(Map(notification));
    }

    private static NotificationResponse Map(Notification notification)
    {
        return new NotificationResponse(
            notification.Id,
            notification.Title,
            notification.Body,
            notification.IsRead,
            notification.CreatedAt,
            notification.Type);
    }
}
