using FixedIT.API.Constants;
using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Jobs;
using FixedIT.API.Extensions;
using FixedIT.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixedIT.API.Controllers;

[ApiController]
[Route("api/jobs")]
public sealed class JobPostingsController(
    IJobSearchService jobSearchService,
    IJobPostingService jobPostingService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<JobSearchResponse>>> GetPage(
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await jobPostingService.GetPageAsync(request, cancellationToken));
    }

    [AllowAnonymous]
    [HttpGet("search")]
    public async Task<ActionResult<PagedResponse<JobSearchResponse>>> Search(
        [FromQuery] JobSearchRequest filters,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await jobSearchService.SearchAsync(
            filters,
            request,
            cancellationToken));
    }

    [Authorize(Roles = RoleNames.Client)]
    [HttpGet("my")]
    public async Task<ActionResult<PagedResponse<JobSearchResponse>>> GetMine(
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await jobPostingService.GetMineAsync(
            User.GetUserId(),
            request,
            cancellationToken));
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<JobPostingDetailResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        return Ok(await jobPostingService.GetByIdAsync(id, cancellationToken));
    }

    [Authorize(Roles = RoleNames.Client)]
    [HttpPost]
    public async Task<ActionResult<JobPostingDetailResponse>> Create(
        CreateJobPostingRequest request,
        CancellationToken cancellationToken)
    {
        var response = await jobPostingService.CreateAsync(
            User.GetUserId(),
            request,
            cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [Authorize(Roles = RoleNames.Client)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<JobPostingDetailResponse>> Update(
        int id,
        UpdateJobPostingRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await jobPostingService.UpdateAsync(
            User.GetUserId(),
            id,
            request,
            cancellationToken));
    }

    [Authorize(Roles = RoleNames.Client)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        await jobPostingService.DeleteAsync(
            User.GetUserId(),
            id,
            cancellationToken);
        return NoContent();
    }
}
