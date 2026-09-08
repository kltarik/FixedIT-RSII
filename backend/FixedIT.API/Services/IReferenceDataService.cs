using FixedIT.API.DTOs.Admin;
using FixedIT.API.DTOs.Common;

namespace FixedIT.API.Services;

public interface IReferenceDataService
{
    Task<PagedResponse<CityOptionResponse>> GetCityOptionsAsync(PagedRequest request, CancellationToken cancellationToken);
    Task<PagedResponse<ReferenceOptionResponse>> GetCategoryOptionsAsync(PagedRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ReservationStatusOptionResponse>> GetActiveReservationStatusOptionsAsync(CancellationToken cancellationToken);

    Task<PagedResponse<CountryResponse>> GetCountriesAsync(ReferenceDataFilterRequest filters, PagedRequest request, CancellationToken cancellationToken);
    Task<CountryResponse> CreateCountryAsync(SaveCountryRequest request, CancellationToken cancellationToken);
    Task<CountryResponse> UpdateCountryAsync(int id, SaveCountryRequest request, CancellationToken cancellationToken);
    Task DeleteCountryAsync(int id, CancellationToken cancellationToken);

    Task<PagedResponse<CityResponse>> GetCitiesAsync(ReferenceDataFilterRequest filters, PagedRequest request, CancellationToken cancellationToken);
    Task<CityResponse> CreateCityAsync(SaveCityRequest request, CancellationToken cancellationToken);
    Task<CityResponse> UpdateCityAsync(int id, SaveCityRequest request, CancellationToken cancellationToken);
    Task DeleteCityAsync(int id, CancellationToken cancellationToken);

    Task<PagedResponse<CategoryResponse>> GetCategoriesAsync(ReferenceDataFilterRequest filters, PagedRequest request, CancellationToken cancellationToken);
    Task<CategoryResponse> CreateCategoryAsync(SaveCategoryRequest request, CancellationToken cancellationToken);
    Task<CategoryResponse> UpdateCategoryAsync(int id, SaveCategoryRequest request, CancellationToken cancellationToken);
    Task DeleteCategoryAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ReservationStatusDefinitionResponse>> GetReservationStatusesAsync(CancellationToken cancellationToken);
    Task<ReservationStatusDefinitionResponse> CreateReservationStatusAsync(SaveReservationStatusDefinitionRequest request, CancellationToken cancellationToken);
    Task<ReservationStatusDefinitionResponse> UpdateReservationStatusAsync(int id, UpdateReservationStatusDefinitionRequest request, CancellationToken cancellationToken);
    Task DeleteReservationStatusAsync(int id, CancellationToken cancellationToken);
}
