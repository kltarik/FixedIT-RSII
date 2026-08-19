using FixedIT.API.DTOs.Common;

namespace FixedIT.API.Services;

public interface IReferenceDataService
{
    Task<ReferenceDataResponse> GetAsync(CancellationToken cancellationToken);
}
