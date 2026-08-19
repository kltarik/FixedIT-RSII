using FixedIT.API.DTOs.Admin;
using FixedIT.API.DTOs.Common;

namespace FixedIT.API.Services;

public interface IAdminUserService
{
    Task<PagedResponse<AdminUserResponse>> GetPageAsync(
        PagedRequest request,
        CancellationToken cancellationToken);

    Task<AdminUserResponse> SetActiveAsync(
        string adminUserId,
        string userId,
        bool isActive,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        string adminUserId,
        string userId,
        CancellationToken cancellationToken);
}
