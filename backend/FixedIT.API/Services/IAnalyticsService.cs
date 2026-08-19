using FixedIT.API.DTOs.Reports;

namespace FixedIT.API.Services;

public interface IAnalyticsService
{
    Task<AdminStatsResponse> GetAdminStatsAsync(CancellationToken cancellationToken);
}
