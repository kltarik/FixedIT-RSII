using FixedIT.API.Constants;
using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Professionals;
using FixedIT.API.Extensions;
using FixedIT.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixedIT.API.Controllers;

[ApiController]
[Authorize]
[Route("api/professionals")]
public sealed class ProfessionalsController(
    IProfessionalService professionalService,
    IRecommendationActivityService recommendationActivityService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProfessionalSummaryResponse>>> GetPage(
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await professionalService.GetPageAsync(request, cancellationToken));
    }

    [HttpGet("search")]
    public async Task<ActionResult<PagedResponse<ProfessionalSummaryResponse>>> Search(
        [FromQuery] ProfessionalSearchRequest filters,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        var response = await professionalService.SearchAsync(
            filters,
            request,
            cancellationToken);
        if (filters.CategoryId.HasValue && IsAuthenticatedClient())
        {
            await recommendationActivityService.RecordCategorySearchAsync(
                User.GetUserId(),
                filters.CategoryId.Value,
                cancellationToken);
        }

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProfessionalDetailResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var response = await professionalService.GetByIdAsync(id, cancellationToken);
        if (IsAuthenticatedClient())
        {
            await recommendationActivityService.RecordProfileViewAsync(
                User.GetUserId(),
                id,
                cancellationToken);
        }

        return Ok(response);
    }

    [Authorize(Roles = RoleNames.Professional)]
    [HttpGet("my-profile")]
    public async Task<ActionResult<ProfessionalDetailResponse>> GetMyProfile(
        CancellationToken cancellationToken)
    {
        return Ok(await professionalService.GetMyProfileAsync(
            User.GetUserId(),
            cancellationToken));
    }

    [Authorize(Roles = RoleNames.Professional)]
    [HttpPut("my-profile")]
    public async Task<ActionResult<ProfessionalDetailResponse>> UpdateMyProfile(
        UpdateProfessionalProfileRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await professionalService.UpdateMyProfileAsync(
            User.GetUserId(),
            request,
            cancellationToken));
    }

    [Authorize(Roles = RoleNames.Professional)]
    [Consumes("multipart/form-data")]
    [HttpPost("portfolio")]
    public async Task<ActionResult<PortfolioItemResponse>> AddPortfolioItem(
        [FromForm] CreatePortfolioItemRequest request,
        CancellationToken cancellationToken)
    {
        var response = await professionalService.AddPortfolioItemAsync(
            User.GetUserId(),
            request,
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize(Roles = RoleNames.Professional)]
    [Consumes("multipart/form-data")]
    [HttpPut("portfolio/{id:int}")]
    public async Task<ActionResult<PortfolioItemResponse>> UpdatePortfolioItem(
        int id,
        [FromForm] UpdatePortfolioItemRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await professionalService.UpdatePortfolioItemAsync(
            User.GetUserId(),
            id,
            request,
            cancellationToken));
    }

    [Authorize(Roles = RoleNames.Professional)]
    [HttpDelete("portfolio/{id:int}")]
    public async Task<IActionResult> DeletePortfolioItem(
        int id,
        CancellationToken cancellationToken)
    {
        await professionalService.DeletePortfolioItemAsync(
            User.GetUserId(),
            id,
            cancellationToken);
        return NoContent();
    }

    private bool IsAuthenticatedClient()
    {
        return User.Identity?.IsAuthenticated == true
            && User.IsInRole(RoleNames.Client);
    }
}
