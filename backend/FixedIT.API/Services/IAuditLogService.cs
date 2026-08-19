using FixedIT.API.DTOs.AuditLogs;
using FixedIT.API.DTOs.Common;

namespace FixedIT.API.Services;

public interface IAuditLogService
{
    Task<PagedResponse<AuditLogResponse>> GetPageAsync(
        AuditLogFilterRequest filters,
        PagedRequest request,
        CancellationToken cancellationToken);
}
