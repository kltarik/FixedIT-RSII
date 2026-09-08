using FixedIT.API.DTOs.Common;
using FixedIT.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixedIT.API.Controllers;

[ApiController]
[Authorize]
[Route("api/reference-data")]
public sealed class ReferenceDataController(IReferenceDataService referenceDataService)
    : ControllerBase
{
    [HttpGet("cities")]
    public async Task<ActionResult<PagedResponse<CityOptionResponse>>> GetCities(
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await referenceDataService.GetCityOptionsAsync(request, cancellationToken));
    }

    [HttpGet("categories")]
    public async Task<ActionResult<PagedResponse<ReferenceOptionResponse>>> GetCategories(
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await referenceDataService.GetCategoryOptionsAsync(request, cancellationToken));
    }
}
