using FixedIT.API.DTOs.Common;
using FixedIT.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixedIT.API.Controllers;

[ApiController]
[Route("api/reference-data")]
public sealed class ReferenceDataController(IReferenceDataService referenceDataService)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ReferenceDataResponse>> Get(
        CancellationToken cancellationToken)
    {
        return Ok(await referenceDataService.GetAsync(cancellationToken));
    }
}
