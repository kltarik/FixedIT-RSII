using FixedIT.API.Constants;
using FixedIT.API.DTOs.Admin;
using FixedIT.API.DTOs.Common;
using FixedIT.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixedIT.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
[Route("api/admin/reference-data")]
public sealed class AdminReferenceDataController(IReferenceDataService referenceDataService)
    : ControllerBase
{
    [HttpGet("countries")]
    public async Task<ActionResult<PagedResponse<CountryResponse>>> GetCountries(
        [FromQuery] ReferenceDataFilterRequest filters,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken) =>
        Ok(await referenceDataService.GetCountriesAsync(filters, request, cancellationToken));

    [HttpPost("countries")]
    public async Task<ActionResult<CountryResponse>> CreateCountry(
        SaveCountryRequest request,
        CancellationToken cancellationToken)
    {
        var country = await referenceDataService.CreateCountryAsync(request, cancellationToken);
        return Created($"api/admin/reference-data/countries/{country.Id}", country);
    }

    [HttpPut("countries/{id:int}")]
    public async Task<ActionResult<CountryResponse>> UpdateCountry(
        int id,
        SaveCountryRequest request,
        CancellationToken cancellationToken) =>
        Ok(await referenceDataService.UpdateCountryAsync(id, request, cancellationToken));

    [HttpDelete("countries/{id:int}")]
    public async Task<IActionResult> DeleteCountry(int id, CancellationToken cancellationToken)
    {
        await referenceDataService.DeleteCountryAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("cities")]
    public async Task<ActionResult<PagedResponse<CityResponse>>> GetCities(
        [FromQuery] ReferenceDataFilterRequest filters,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken) =>
        Ok(await referenceDataService.GetCitiesAsync(filters, request, cancellationToken));

    [HttpPost("cities")]
    public async Task<ActionResult<CityResponse>> CreateCity(
        SaveCityRequest request,
        CancellationToken cancellationToken)
    {
        var city = await referenceDataService.CreateCityAsync(request, cancellationToken);
        return Created($"api/admin/reference-data/cities/{city.Id}", city);
    }

    [HttpPut("cities/{id:int}")]
    public async Task<ActionResult<CityResponse>> UpdateCity(
        int id,
        SaveCityRequest request,
        CancellationToken cancellationToken) =>
        Ok(await referenceDataService.UpdateCityAsync(id, request, cancellationToken));

    [HttpDelete("cities/{id:int}")]
    public async Task<IActionResult> DeleteCity(int id, CancellationToken cancellationToken)
    {
        await referenceDataService.DeleteCityAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("categories")]
    public async Task<ActionResult<PagedResponse<CategoryResponse>>> GetCategories(
        [FromQuery] ReferenceDataFilterRequest filters,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken) =>
        Ok(await referenceDataService.GetCategoriesAsync(filters, request, cancellationToken));

    [HttpPost("categories")]
    public async Task<ActionResult<CategoryResponse>> CreateCategory(
        SaveCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await referenceDataService.CreateCategoryAsync(request, cancellationToken);
        return Created($"api/admin/reference-data/categories/{category.Id}", category);
    }

    [HttpPut("categories/{id:int}")]
    public async Task<ActionResult<CategoryResponse>> UpdateCategory(
        int id,
        SaveCategoryRequest request,
        CancellationToken cancellationToken) =>
        Ok(await referenceDataService.UpdateCategoryAsync(id, request, cancellationToken));

    [HttpDelete("categories/{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken cancellationToken)
    {
        await referenceDataService.DeleteCategoryAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("reservation-statuses")]
    public async Task<ActionResult<IReadOnlyCollection<ReservationStatusDefinitionResponse>>> GetReservationStatuses(
        CancellationToken cancellationToken) =>
        Ok(await referenceDataService.GetReservationStatusesAsync(cancellationToken));

    [HttpPut("reservation-statuses/{id:int}")]
    public async Task<ActionResult<ReservationStatusDefinitionResponse>> UpdateReservationStatus(
        int id,
        UpdateReservationStatusDefinitionRequest request,
        CancellationToken cancellationToken) =>
        Ok(await referenceDataService.UpdateReservationStatusAsync(id, request, cancellationToken));

}
