using FixedIT.API.Constants;
using FixedIT.API.CustomExceptions;
using FixedIT.API.Data;
using FixedIT.API.DTOs.Common;
using FixedIT.API.DTOs.Professionals;
using FixedIT.API.Models;
using FixedIT.API.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FixedIT.API.Services;

public sealed class ProfessionalService(
    AppDbContext db,
    IPaginationService paginationService,
    IFileUploadService fileUploadService) : IProfessionalService
{
    private static readonly Expression<Func<ProfessionalProfile, ProfessionalSummaryResponse>>
        SummaryProjection = profile => new ProfessionalSummaryResponse(
            profile.Id,
            profile.UserId,
            profile.User.FirstName,
            profile.User.LastName,
            profile.User.ProfilePictureUrl,
            profile.User.CityId,
            profile.User.City.Name,
            profile.Bio,
            profile.HourlyRate,
            profile.YearsOfExperience,
            profile.IsVerified,
            profile.User.IsActive,
            profile.AverageRating,
            profile.ProfessionalCategories
                .OrderBy(link => link.Category.Name)
                .Select(link => new CategorySummaryResponse(
                    link.CategoryId,
                    link.Category.Name))
                .ToArray());

    public async Task<PagedResponse<ProfessionalSummaryResponse>> GetPageAsync(
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        var page = paginationService.Normalize(request);
        var query = db.ProfessionalProfiles
            .AsNoTracking()
            .Where(profile => profile.IsVerified && profile.User.IsActive);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(profile => profile.AverageRating)
            .ThenBy(profile => profile.User.LastName)
            .ThenBy(profile => profile.User.FirstName)
            .ThenBy(profile => profile.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(SummaryProjection)
            .ToListAsync(cancellationToken);

        return new PagedResponse<ProfessionalSummaryResponse>(
            items,
            total,
            page.Page,
            page.PageSize);
    }

    public async Task<PagedResponse<ProfessionalSummaryResponse>> SearchAsync(
        ProfessionalSearchRequest filters,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        return await SearchPageAsync(
            db.ProfessionalProfiles
                .AsNoTracking()
                .Where(profile => profile.IsVerified && profile.User.IsActive),
            filters,
            request,
            cancellationToken);
    }

    public Task<PagedResponse<ProfessionalSummaryResponse>> GetAdminPageAsync(
        ProfessionalSearchRequest filters,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        return SearchPageAsync(
            db.ProfessionalProfiles.IgnoreQueryFilters().AsNoTracking(),
            filters,
            request,
            cancellationToken);
    }

    private async Task<PagedResponse<ProfessionalSummaryResponse>> SearchPageAsync(
        IQueryable<ProfessionalProfile> query,
        ProfessionalSearchRequest filters,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        var page = paginationService.Normalize(request);

        if (!string.IsNullOrWhiteSpace(filters.Name))
        {
            var name = filters.Name.Trim();
            query = query.Where(profile =>
                profile.User.FirstName.Contains(name)
                || profile.User.LastName.Contains(name)
                || (profile.User.FirstName + " " + profile.User.LastName).Contains(name));
        }

        if (filters.CategoryId.HasValue)
        {
            query = query.Where(profile => profile.ProfessionalCategories
                .Any(link => link.CategoryId == filters.CategoryId.Value));
        }

        if (filters.CityId.HasValue)
        {
            query = query.Where(profile => profile.User.CityId == filters.CityId.Value);
        }

        if (filters.MinRating.HasValue)
        {
            query = query.Where(profile => profile.AverageRating >= filters.MinRating.Value);
        }

        if (filters.MaxHourlyRate.HasValue)
        {
            query = query.Where(profile => profile.HourlyRate <= filters.MaxHourlyRate.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var sortedQuery = ApplySearchSort(query, filters.SortBy, filters.SortOrder);
        var items = await sortedQuery
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(SummaryProjection)
            .ToListAsync(cancellationToken);

        return new PagedResponse<ProfessionalSummaryResponse>(
            items,
            total,
            page.Page,
            page.PageSize);
    }

    public Task<ProfessionalDetailResponse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return GetDetailAsync(
            profile => profile.Id
                == id && profile.IsVerified && profile.User.IsActive,
            "Profil profesionalca nije pronađen.",
            cancellationToken);
    }

    public async Task<ProfessionalDetailResponse> SetVerificationAsync(
        int id,
        bool isVerified,
        CancellationToken cancellationToken)
    {
        var profile = await db.ProfessionalProfiles
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Profil profesionalca nije pronađen.");

        profile.IsVerified = isVerified;
        await db.SaveChangesAsync(cancellationToken);

        return await GetDetailAsync(
            item => item.Id == profile.Id,
            "Profil profesionalca nije pronađen.",
            cancellationToken);
    }

    public async Task<ProfessionalDetailResponse> AdminUpdateAsync(
        int id,
        AdminUpdateProfessionalProfileRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await db.ProfessionalProfiles
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Profil profesionalca nije pronađen.");
        await UpdateProfileValuesAsync(
            profile,
            request.Bio,
            request.HourlyRate,
            request.YearsOfExperience,
            request.CategoryIds,
            cancellationToken);
        return await GetDetailAsync(
            item => item.Id == profile.Id,
            "Profil profesionalca nije pronađen.",
            cancellationToken);
    }

    public Task<ProfessionalDetailResponse> GetMyProfileAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        return GetDetailAsync(
            profile => profile.UserId == userId,
            "Profil profesionalca nije pronađen.",
            cancellationToken);
    }

    public async Task<ProfessionalDetailResponse> UpdateMyProfileAsync(
        string userId,
        UpdateProfessionalProfileRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await db.ProfessionalProfiles
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Profil profesionalca nije pronađen.");
        await UpdateProfileValuesAsync(
            profile,
            request.Bio,
            request.HourlyRate,
            request.YearsOfExperience,
            request.CategoryIds,
            cancellationToken);

        return await GetMyProfileAsync(userId, cancellationToken);
    }

    private async Task UpdateProfileValuesAsync(
        ProfessionalProfile profile,
        string bio,
        decimal hourlyRate,
        int yearsOfExperience,
        int[] requestedCategoryIds,
        CancellationToken cancellationToken)
    {
        var categoryIds = requestedCategoryIds.Distinct().ToArray();
        if (categoryIds.Any(id => id <= 0))
        {
            throw new BusinessException("Identifikatori kategorija moraju biti pozitivni brojevi.");
        }

        var existingCategoryCount = await db.Categories
            .CountAsync(category => categoryIds.Contains(category.Id), cancellationToken);
        if (existingCategoryCount != categoryIds.Length)
        {
            throw new BusinessException("Jedna ili više odabranih kategorija ne postoje.");
        }

        var currentLinks = await db.ProfessionalCategories
            .Where(link => link.ProfessionalProfileId == profile.Id)
            .ToListAsync(cancellationToken);
        db.ProfessionalCategories.RemoveRange(currentLinks);
        db.ProfessionalCategories.AddRange(categoryIds.Select(categoryId => new ProfessionalCategory
        {
            ProfessionalProfileId = profile.Id,
            CategoryId = categoryId
        }));

        profile.Bio = bio.Trim();
        profile.HourlyRate = hourlyRate;
        profile.YearsOfExperience = yearsOfExperience;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PortfolioItemResponse> AddPortfolioItemAsync(
        string userId,
        CreatePortfolioItemRequest request,
        CancellationToken cancellationToken)
    {
        var profileId = await GetOwnedProfileIdAsync(userId, cancellationToken);
        var storedFile = await fileUploadService.SaveImageAsync(
            request.Image,
            FileStorageConstants.PortfolioFolder,
            cancellationToken);
        var item = new PortfolioItem
        {
            ProfessionalProfileId = profileId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            ImageUrl = storedFile.PublicUrl,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            db.PortfolioItems.Add(item);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await fileUploadService.DeleteImageAsync(storedFile.PublicUrl);
            throw;
        }

        return MapPortfolioItem(item);
    }

    public async Task<PortfolioItemResponse> UpdatePortfolioItemAsync(
        string userId,
        int id,
        UpdatePortfolioItemRequest request,
        CancellationToken cancellationToken)
    {
        var item = await db.PortfolioItems
            .SingleOrDefaultAsync(
                portfolio => portfolio.Id == id
                    && portfolio.ProfessionalProfile.UserId == userId,
                cancellationToken)
            ?? throw new NotFoundException("Stavka portfolija nije pronađena.");
        StoredFileResult? storedFile = null;
        var oldImageUrl = item.ImageUrl;
        if (request.Image is not null)
        {
            storedFile = await fileUploadService.SaveImageAsync(
                request.Image,
                FileStorageConstants.PortfolioFolder,
                cancellationToken);
            item.ImageUrl = storedFile.PublicUrl;
        }

        item.Title = request.Title.Trim();
        item.Description = request.Description.Trim();
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (storedFile is not null)
            {
                await fileUploadService.DeleteImageAsync(storedFile.PublicUrl);
            }

            throw;
        }

        if (storedFile is not null)
        {
            await fileUploadService.DeleteImageAsync(oldImageUrl);
        }

        return MapPortfolioItem(item);
    }

    public async Task DeletePortfolioItemAsync(
        string userId,
        int id,
        CancellationToken cancellationToken)
    {
        var item = await db.PortfolioItems
            .SingleOrDefaultAsync(
                portfolio => portfolio.Id == id
                    && portfolio.ProfessionalProfile.UserId == userId,
                cancellationToken)
            ?? throw new NotFoundException("Stavka portfolija nije pronađena.");

        db.PortfolioItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        await fileUploadService.DeleteImageAsync(item.ImageUrl);
    }

    private async Task<int> GetOwnedProfileIdAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        return await db.ProfessionalProfiles
            .Where(profile => profile.UserId == userId)
            .Select(profile => (int?)profile.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Profil profesionalca nije pronađen.");
    }

    private static IOrderedQueryable<ProfessionalProfile> ApplySearchSort(
        IQueryable<ProfessionalProfile> query,
        string sortBy,
        string sortOrder)
    {
        var descending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        return sortBy.ToLowerInvariant() switch
        {
            "rating" when descending => query
                .OrderByDescending(profile => profile.AverageRating)
                .ThenByDescending(profile => profile.Id),
            "rating" => query
                .OrderBy(profile => profile.AverageRating)
                .ThenBy(profile => profile.Id),
            "price" when descending => query
                .OrderByDescending(profile => profile.HourlyRate)
                .ThenByDescending(profile => profile.Id),
            "price" => query
                .OrderBy(profile => profile.HourlyRate)
                .ThenBy(profile => profile.Id),
            "name" when descending => query
                .OrderByDescending(profile => profile.User.LastName)
                .ThenByDescending(profile => profile.User.FirstName)
                .ThenByDescending(profile => profile.Id),
            "name" => query
                .OrderBy(profile => profile.User.LastName)
                .ThenBy(profile => profile.User.FirstName)
                .ThenBy(profile => profile.Id),
            "completed" when descending => query
                .OrderByDescending(profile => profile.Reservations.Count(
                    reservation => reservation.Status == ReservationStatus.Completed))
                .ThenByDescending(profile => profile.AverageRating)
                .ThenByDescending(profile => profile.Id),
            "completed" => query
                .OrderBy(profile => profile.Reservations.Count(
                    reservation => reservation.Status == ReservationStatus.Completed))
                .ThenBy(profile => profile.AverageRating)
                .ThenBy(profile => profile.Id),
            _ => throw new BusinessException("Odabrani način sortiranja profesionalaca nije podržan.")
        };
    }

    private async Task<ProfessionalDetailResponse> GetDetailAsync(
        System.Linq.Expressions.Expression<Func<ProfessionalProfile, bool>> predicate,
        string notFoundMessage,
        CancellationToken cancellationToken)
    {
        return await db.ProfessionalProfiles
            .AsNoTracking()
            .Where(predicate)
            .Select(profile => new ProfessionalDetailResponse(
                profile.Id,
                profile.UserId,
                profile.User.FirstName,
                profile.User.LastName,
                profile.User.ProfilePictureUrl,
                profile.User.CityId,
                profile.User.City.Name,
                profile.Bio,
                profile.HourlyRate,
                profile.YearsOfExperience,
                profile.IsVerified,
                profile.User.IsActive,
                profile.AverageRating,
                profile.ProfessionalCategories
                    .OrderBy(link => link.Category.Name)
                    .Select(link => new CategorySummaryResponse(
                        link.CategoryId,
                        link.Category.Name))
                    .ToArray(),
                profile.PortfolioItems
                    .OrderByDescending(item => item.CreatedAt)
                    .ThenByDescending(item => item.Id)
                    .Select(item => new PortfolioItemResponse(
                        item.Id,
                        item.Title,
                        item.Description,
                        item.ImageUrl,
                        item.CreatedAt))
                    .ToArray()))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(notFoundMessage);
    }

    private static PortfolioItemResponse MapPortfolioItem(PortfolioItem item)
    {
        return new PortfolioItemResponse(
            item.Id,
            item.Title,
            item.Description,
            item.ImageUrl,
            item.CreatedAt);
    }
}
