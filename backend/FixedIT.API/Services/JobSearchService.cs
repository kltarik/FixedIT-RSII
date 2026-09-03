using FixedIT.API.Data;
using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Jobs;
using Microsoft.EntityFrameworkCore;

namespace FixedIT.API.Services;

public sealed class JobSearchService(
    AppDbContext db,
    IPaginationService paginationService) : IJobSearchService
{
    public async Task<PagedResponse<JobSearchResponse>> SearchAsync(
        JobSearchRequest filters,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        var page = paginationService.Normalize(request);
        var query = db.JobPostings.AsNoTracking();

        if (filters.CategoryId.HasValue)
        {
            query = query.Where(job => job.CategoryId == filters.CategoryId.Value);
        }

        if (filters.CityId.HasValue)
        {
            query = query.Where(job => job.CityId == filters.CityId.Value);
        }

        if (filters.MinBudget.HasValue)
        {
            query = query.Where(job => job.Budget >= filters.MinBudget.Value);
        }

        if (filters.MaxBudget.HasValue)
        {
            query = query.Where(job => job.Budget <= filters.MaxBudget.Value);
        }

        if (filters.Status.HasValue)
        {
            query = query.Where(job => job.Status == filters.Status.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(job => job.CreatedAt)
            .ThenByDescending(job => job.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(job => new JobSearchResponse(
                job.Id,
                job.Title,
                job.Description,
                job.Budget,
                job.Status,
                job.CreatedAt,
                job.CategoryId,
                job.Category.Name,
                job.CityId,
                job.City.Name,
                job.ClientUserId,
                job.ClientUser.FirstName,
                job.ClientUser.LastName,
                job.ClientUser.ProfilePictureUrl,
                job.Images.OrderBy(image => image.Id).Select(image => image.ImageUrl).ToArray()))
            .ToListAsync(cancellationToken);

        return new PagedResponse<JobSearchResponse>(
            items,
            total,
            page.Page,
            page.PageSize);
    }
}
