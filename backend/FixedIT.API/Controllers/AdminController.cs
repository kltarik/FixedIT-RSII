using FixedIT.API.Constants;
using FixedIT.API.DTOs.Admin;
using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Professionals;
using FixedIT.API.Extensions;
using FixedIT.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixedIT.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
[Route("api/admin")]
public sealed class AdminController(
    IAdminUserService adminUserService,
    IProfessionalService professionalService) : ControllerBase
{
    [HttpGet("professionals")]
    public async Task<ActionResult<PagedResponse<ProfessionalSummaryResponse>>> GetProfessionals(
        [FromQuery] ProfessionalSearchRequest filters,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await professionalService.GetAdminPageAsync(filters, request, cancellationToken));
    }

    [HttpGet("users")]
    public async Task<ActionResult<PagedResponse<AdminUserResponse>>> GetUsers(
        [FromQuery] AdminUserFilterRequest filters,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await adminUserService.GetPageAsync(filters, request, cancellationToken));
    }

    [HttpPut("users/{id}/activate")]
    public async Task<ActionResult<AdminUserResponse>> SetUserActive(
        string id,
        SetUserActiveRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await adminUserService.SetActiveAsync(
            User.GetUserId(),
            id,
            request.IsActive!.Value,
            cancellationToken));
    }

    [HttpPut("users/{id}")]
    public async Task<ActionResult<AdminUserResponse>> UpdateUser(
        string id,
        UpdateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await adminUserService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(
        string id,
        CancellationToken cancellationToken)
    {
        await adminUserService.DeleteAsync(
            User.GetUserId(),
            id,
            cancellationToken);
        return NoContent();
    }

    [HttpPut("professionals/{id:int}/verification")]
    public async Task<ActionResult<ProfessionalDetailResponse>> SetProfessionalVerification(
        int id,
        SetProfessionalVerificationRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await professionalService.SetVerificationAsync(
            id,
            request.IsVerified!.Value,
            cancellationToken));
    }

    [HttpPut("professionals/{id:int}")]
    public async Task<ActionResult<ProfessionalDetailResponse>> UpdateProfessional(
        int id,
        AdminUpdateProfessionalProfileRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await professionalService.AdminUpdateAsync(id, request, cancellationToken));
    }
}
