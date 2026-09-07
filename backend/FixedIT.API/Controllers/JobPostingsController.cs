using FixedIT.API.Constants;
using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Jobs;
using FixedIT.API.Extensions;
using FixedIT.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixedIT.API.Controllers;

[ApiController]
[Authorize]
[Route("api/jobs")]
public sealed class JobPostingsController(
    IJobSearchService jobSearchService,
    IJobPostingService jobPostingService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<JobSearchResponse>>> GetPage(
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await jobPostingService.GetPageAsync(request, cancellationToken));
    }

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

    [Authorize(Roles = RoleNames.Client)]
    [Consumes("multipart/form-data")]
    [HttpPost("{id:int}/images")]
    public async Task<ActionResult<JobPostingImageResponse>> AddImage(
        int id,
        [FromForm] AddJobPostingImageRequest request,
        CancellationToken cancellationToken)
    {
        var image = await jobPostingService.AddImageAsync(
            User.GetUserId(),
            id,
            request,
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, image);
    }

    [Authorize(Roles = RoleNames.Client)]
    [HttpDelete("{id:int}/images/{imageId:int}")]
    public async Task<IActionResult> DeleteImage(
        int id,
        int imageId,
        CancellationToken cancellationToken)
    {
        await jobPostingService.DeleteImageAsync(
            User.GetUserId(),
            id,
            imageId,
            cancellationToken);
        return NoContent();
    }
}
