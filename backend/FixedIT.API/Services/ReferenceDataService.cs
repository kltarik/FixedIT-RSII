using FixedIT.API.Data;
using FixedIT.API.DTOs.Common;
using Microsoft.EntityFrameworkCore;

namespace FixedIT.API.Services;

public sealed class ReferenceDataService(AppDbContext db) : IReferenceDataService
{
    public async Task<ReferenceDataResponse> GetAsync(CancellationToken cancellationToken)
    {
        var cities = await db.Cities
            .AsNoTracking()
            .OrderBy(city => city.Name)
            .Select(city => new ReferenceOptionResponse(city.Id, city.Name))
            .ToListAsync(cancellationToken);
        var categories = await db.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new ReferenceOptionResponse(category.Id, category.Name))
            .ToListAsync(cancellationToken);

        return new ReferenceDataResponse(cities, categories);
    }
}
