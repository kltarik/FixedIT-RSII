using System.Data;
using System.Linq.Expressions;
using FixedIT.API.Constants;
using FixedIT.API.CustomExceptions;
using FixedIT.API.Data;
using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Jobs;
using FixedIT.API.Models;
using FixedIT.API.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FixedIT.API.Services;

public sealed class JobPostingService(
    AppDbContext db,
    IPaginationService paginationService,
    IFileUploadService fileUploadService) : IJobPostingService
{
    private const string LockedJobPostingsSql =
        "SELECT * FROM [JobPostings] WITH (UPDLOCK, HOLDLOCK)";

    private static readonly Expression<Func<JobPosting, JobSearchResponse>> SummaryProjection =
        job => new JobSearchResponse(
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
            job.Images.OrderBy(image => image.Id).Select(image => image.ImageUrl).ToArray());

    public Task<PagedResponse<JobSearchResponse>> GetPageAsync(
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        return GetPageAsync(
            db.JobPostings
                .AsNoTracking()
                .Where(job => job.ClientUser.IsActive),
            request,
            cancellationToken);
    }

    public async Task<JobPostingDetailResponse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var job = await db.JobPostings
            .AsNoTracking()
            .Where(item => item.Id == id && item.ClientUser.IsActive)
            .Select(item => new
            {
                item.Id,
                item.Title,
                item.Description,
                item.Budget,
                item.Status,
                item.CreatedAt,
                item.CategoryId,
                CategoryName = item.Category.Name,
                item.CityId,
                CityName = item.City.Name,
                item.ClientUserId,
                ClientFirstName = item.ClientUser.FirstName,
                ClientLastName = item.ClientUser.LastName,
                ClientProfilePictureUrl = item.ClientUser.ProfilePictureUrl,
                Images = item.Images
                    .OrderBy(image => image.Id)
                    .Select(image => new JobPostingImageResponse(
                        image.Id,
                        image.ImageUrl,
                        image.CreatedAt))
                    .ToArray()
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Oglas za posao nije pronađen.");
        var offerCount = await db.JobOffers
            .IgnoreQueryFilters()
            .CountAsync(offer => offer.JobPostingId == id, cancellationToken);

        return new JobPostingDetailResponse(
            job.Id,
            job.Title,
            job.Description,
            job.Budget,
            job.Status,
            job.CreatedAt,
            job.CategoryId,
            job.CategoryName,
            job.CityId,
            job.CityName,
            job.ClientUserId,
            job.ClientFirstName,
            job.ClientLastName,
            job.ClientProfilePictureUrl,
            offerCount,
            job.Images);
    }

    public Task<PagedResponse<JobSearchResponse>> GetMineAsync(
        string clientUserId,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        var query = db.JobPostings
            .AsNoTracking()
            .Where(job => job.ClientUserId == clientUserId);
        return GetPageAsync(query, request, cancellationToken);
    }

    public async Task<JobPostingDetailResponse> CreateAsync(
        string clientUserId,
        CreateJobPostingRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateReferencesAsync(request.CategoryId, request.CityId, cancellationToken);
        var job = new JobPosting
        {
            ClientUserId = clientUserId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            CategoryId = request.CategoryId,
            CityId = request.CityId,
            Budget = request.Budget,
            Status = JobPostingStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        db.JobPostings.Add(job);
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(job.Id, cancellationToken);
    }

    public async Task<JobPostingDetailResponse> UpdateAsync(
        string clientUserId,
        int id,
        UpdateJobPostingRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var job = await db.JobPostings
            .FromSqlRaw(LockedJobPostingsSql)
            .SingleOrDefaultAsync(
                item => item.Id == id && item.ClientUserId == clientUserId,
                cancellationToken)
            ?? throw new NotFoundException("Oglas za posao nije pronađen.");
        if (job.Status != JobPostingStatus.Open)
        {
            throw new BusinessException("Moguće je izmijeniti samo otvorene oglase za posao.");
        }

        await ValidateReferencesAsync(request.CategoryId, request.CityId, cancellationToken);
        job.Title = request.Title.Trim();
        job.Description = request.Description.Trim();
        job.CategoryId = request.CategoryId;
        job.CityId = request.CityId;
        job.Budget = request.Budget;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(job.Id, cancellationToken);
    }

    public async Task DeleteAsync(
        string clientUserId,
        int id,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var job = await db.JobPostings
            .FromSqlRaw(LockedJobPostingsSql)
            .SingleOrDefaultAsync(
                item => item.Id == id && item.ClientUserId == clientUserId,
                cancellationToken)
            ?? throw new NotFoundException("Oglas za posao nije pronađen.");
        if (await db.JobOffers
            .IgnoreQueryFilters()
            .AnyAsync(offer => offer.JobPostingId == job.Id, cancellationToken))
        {
            throw new BusinessException("Oglas za posao koji ima ponude nije moguće izbrisati.");
        }

        var imageUrls = await db.JobPostingImages
            .Where(image => image.JobPostingId == job.Id)
            .Select(image => image.ImageUrl)
            .ToArrayAsync(cancellationToken);
        db.JobPostings.Remove(job);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        foreach (var imageUrl in imageUrls)
        {
            await fileUploadService.DeleteImageAsync(imageUrl);
        }
    }

    public async Task<JobPostingImageResponse> AddImageAsync(
        string clientUserId,
        int jobPostingId,
        AddJobPostingImageRequest request,
        CancellationToken cancellationToken)
    {
        var ownsJob = await db.JobPostings.AnyAsync(
            job => job.Id == jobPostingId && job.ClientUserId == clientUserId,
            cancellationToken);
        if (!ownsJob)
        {
            throw new NotFoundException("Oglas za posao nije pronađen.");
        }

        var imageCount = await db.JobPostingImages.CountAsync(
            image => image.JobPostingId == jobPostingId,
            cancellationToken);
        if (imageCount >= 5)
        {
            throw new BusinessException("Oglas može sadržavati najviše pet fotografija.");
        }

        var stored = await fileUploadService.SaveImageAsync(
            request.Image,
            FileStorageConstants.JobPostingsFolder,
            cancellationToken);
        var image = new JobPostingImage
        {
            JobPostingId = jobPostingId,
            ImageUrl = stored.PublicUrl,
            CreatedAt = DateTime.UtcNow
        };
        try
        {
            db.JobPostingImages.Add(image);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await fileUploadService.DeleteImageAsync(stored.PublicUrl);
            throw;
        }

        return new JobPostingImageResponse(image.Id, image.ImageUrl, image.CreatedAt);
    }

    public async Task DeleteImageAsync(
        string clientUserId,
        int jobPostingId,
        int imageId,
        CancellationToken cancellationToken)
    {
        var image = await db.JobPostingImages.SingleOrDefaultAsync(
            item => item.Id == imageId
                && item.JobPostingId == jobPostingId
                && item.JobPosting.ClientUserId == clientUserId,
            cancellationToken)
            ?? throw new NotFoundException("Fotografija oglasa nije pronađena.");
        db.JobPostingImages.Remove(image);
        await db.SaveChangesAsync(cancellationToken);
        await fileUploadService.DeleteImageAsync(image.ImageUrl);
    }

    private async Task<PagedResponse<JobSearchResponse>> GetPageAsync(
        IQueryable<JobPosting> query,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        var page = paginationService.Normalize(request);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(job => job.CreatedAt)
            .ThenByDescending(job => job.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(SummaryProjection)
            .ToListAsync(cancellationToken);

        return new PagedResponse<JobSearchResponse>(
            items,
            total,
            page.Page,
            page.PageSize);
    }

    private async Task ValidateReferencesAsync(
        int categoryId,
        int cityId,
        CancellationToken cancellationToken)
    {
        if (!await db.Categories.AnyAsync(category => category.Id == categoryId, cancellationToken))
        {
            throw new NotFoundException("Odabrana kategorija nije pronađena.");
        }

        if (!await db.Cities.AnyAsync(city => city.Id == cityId, cancellationToken))
        {
            throw new NotFoundException("Odabrani grad nije pronađen.");
        }
    }
}
