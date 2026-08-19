using FixedIT.API.Data;
using FixedIT.API.DTOs.AuditLogs;
using FixedIT.API.DTOs.Common;
using FixedIT.API.Models;
using Microsoft.EntityFrameworkCore;

namespace FixedIT.API.Services;

public sealed class AuditLogService(
    AppDbContext db,
    IPaginationService paginationService) : IAuditLogService
{
    public async Task<PagedResponse<AuditLogResponse>> GetPageAsync(
        AuditLogFilterRequest filters,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        var page = paginationService.Normalize(request);
        IQueryable<AuditLog> query = db.AuditLogs
            .IgnoreQueryFilters()
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filters.UserId))
        {
            var userId = filters.UserId.Trim();
            query = query.Where(log => log.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(filters.Action))
        {
            var action = filters.Action.Trim();
            query = query.Where(log => log.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(filters.EntityType))
        {
            var entityType = filters.EntityType.Trim();
            query = query.Where(log => log.EntityType == entityType);
        }

        if (filters.DateFrom.HasValue)
        {
            var dateFromUtc = filters.DateFrom.Value.UtcDateTime;
            query = query.Where(log => log.CreatedAt >= dateFromUtc);
        }

        if (filters.DateTo.HasValue)
        {
            var dateToUtc = filters.DateTo.Value.UtcDateTime;
            query = query.Where(log => log.CreatedAt <= dateToUtc);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(log => log.CreatedAt)
            .ThenByDescending(log => log.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(log => new AuditLogResponse(
                log.Id,
                log.UserId,
                log.Action,
                log.EntityType,
                log.EntityId,
                log.Details,
                log.IpAddress,
                log.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResponse<AuditLogResponse>(
            items,
            total,
            page.Page,
            page.PageSize);
    }
}
