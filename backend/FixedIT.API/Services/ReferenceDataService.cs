using FixedIT.API.CustomExceptions;
using FixedIT.API.Data;
using FixedIT.API.DTOs.Admin;
using FixedIT.API.DTOs.Common;
using FixedIT.API.Models;
using FixedIT.API.Models.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FixedIT.API.Services;

public sealed class ReferenceDataService(
    AppDbContext db,
    IPaginationService paginationService) : IReferenceDataService
{
    public Task<PagedResponse<CityOptionResponse>> GetCityOptionsAsync(
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        return PageAsync(
            db.Cities
            .AsNoTracking()
            .OrderBy(city => city.Name)
            .ThenBy(city => city.Id),
            request,
            city => new CityOptionResponse(city.Id, city.Name, city.CountryId),
            cancellationToken);
    }

    public Task<PagedResponse<ReferenceOptionResponse>> GetCategoryOptionsAsync(
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        return PageAsync(
            db.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ThenBy(category => category.Id),
            request,
            category => new ReferenceOptionResponse(category.Id, category.Name),
            cancellationToken);
    }

    public Task<PagedResponse<CountryResponse>> GetCountriesAsync(
        ReferenceDataFilterRequest filters,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        var query = db.Countries.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var search = filters.Search.Trim();
            query = query.Where(country =>
                country.Name.Contains(search) || country.Code.Contains(search));
        }

        return PageAsync(
            query.OrderBy(country => country.Name).ThenBy(country => country.Id),
            request,
            country => new CountryResponse(country.Id, country.Name, country.Code),
            cancellationToken);
    }

    public async Task<CountryResponse> CreateCountryAsync(
        SaveCountryRequest request,
        CancellationToken cancellationToken)
    {
        var country = new Country
        {
            Name = RequiredText(request.Name, "Naziv države"),
            Code = RequiredText(request.Code, "Oznaka države").ToUpperInvariant()
        };
        db.Countries.Add(country);
        await SaveReferenceDataAsync("Država s istim nazivom ili oznakom već postoji.", cancellationToken);
        return new CountryResponse(country.Id, country.Name, country.Code);
    }

    public async Task<CountryResponse> UpdateCountryAsync(
        int id,
        SaveCountryRequest request,
        CancellationToken cancellationToken)
    {
        var country = await db.Countries.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Država nije pronađena.");
        country.Name = RequiredText(request.Name, "Naziv države");
        country.Code = RequiredText(request.Code, "Oznaka države").ToUpperInvariant();
        await SaveReferenceDataAsync("Država s istim nazivom ili oznakom već postoji.", cancellationToken);
        return new CountryResponse(country.Id, country.Name, country.Code);
    }

    public async Task DeleteCountryAsync(int id, CancellationToken cancellationToken)
    {
        var country = await db.Countries.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Država nije pronađena.");
        if (await db.Cities.AnyAsync(city => city.CountryId == id, cancellationToken))
        {
            throw new BusinessException("Državu nije moguće obrisati dok sadrži gradove.");
        }

        db.Countries.Remove(country);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<PagedResponse<CityResponse>> GetCitiesAsync(
        ReferenceDataFilterRequest filters,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        var query = db.Cities.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var search = filters.Search.Trim();
            query = query.Where(city =>
                city.Name.Contains(search) || city.Country.Name.Contains(search));
        }

        return PageAsync(
            query.OrderBy(city => city.Name).ThenBy(city => city.Id),
            request,
            city => new CityResponse(city.Id, city.Name, city.CountryId, city.Country.Name),
            cancellationToken);
    }

    public async Task<CityResponse> CreateCityAsync(
        SaveCityRequest request,
        CancellationToken cancellationToken)
    {
        var country = await db.Countries
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.CountryId, cancellationToken)
            ?? throw new NotFoundException("Država nije pronađena.");
        var city = new City
        {
            Name = RequiredText(request.Name, "Naziv grada"),
            CountryId = country.Id
        };
        db.Cities.Add(city);
        await SaveReferenceDataAsync("Grad s istim nazivom već postoji u odabranoj državi.", cancellationToken);
        return new CityResponse(city.Id, city.Name, country.Id, country.Name);
    }

    public async Task<CityResponse> UpdateCityAsync(
        int id,
        SaveCityRequest request,
        CancellationToken cancellationToken)
    {
        var city = await db.Cities.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Grad nije pronađen.");
        var country = await db.Countries
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.CountryId, cancellationToken)
            ?? throw new NotFoundException("Država nije pronađena.");
        city.Name = RequiredText(request.Name, "Naziv grada");
        city.CountryId = country.Id;
        await SaveReferenceDataAsync("Grad s istim nazivom već postoji u odabranoj državi.", cancellationToken);
        return new CityResponse(city.Id, city.Name, country.Id, country.Name);
    }

    public async Task DeleteCityAsync(int id, CancellationToken cancellationToken)
    {
        var city = await db.Cities.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Grad nije pronađen.");
        var inUse = await db.Users.IgnoreQueryFilters().AnyAsync(user => user.CityId == id, cancellationToken)
            || await db.JobPostings.IgnoreQueryFilters().AnyAsync(job => job.CityId == id, cancellationToken);
        if (inUse)
        {
            throw new BusinessException("Grad nije moguće obrisati dok ga koriste korisnici ili oglasi.");
        }

        db.Cities.Remove(city);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<PagedResponse<CategoryResponse>> GetCategoriesAsync(
        ReferenceDataFilterRequest filters,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        var query = db.Categories.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var search = filters.Search.Trim();
            query = query.Where(category =>
                category.Name.Contains(search) || category.Description.Contains(search));
        }

        return PageAsync(
            query.OrderBy(category => category.Name).ThenBy(category => category.Id),
            request,
            category => new CategoryResponse(
                category.Id,
                category.Name,
                category.Description,
                category.IconUrl),
            cancellationToken);
    }

    public async Task<CategoryResponse> CreateCategoryAsync(
        SaveCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = new Category
        {
            Name = RequiredText(request.Name, "Naziv kategorije"),
            Description = RequiredText(request.Description, "Opis kategorije"),
            IconUrl = OptionalText(request.IconUrl)
        };
        db.Categories.Add(category);
        await SaveReferenceDataAsync("Kategorija s istim nazivom već postoji.", cancellationToken);
        return MapCategory(category);
    }

    public async Task<CategoryResponse> UpdateCategoryAsync(
        int id,
        SaveCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await db.Categories.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Kategorija nije pronađena.");
        category.Name = RequiredText(request.Name, "Naziv kategorije");
        category.Description = RequiredText(request.Description, "Opis kategorije");
        category.IconUrl = OptionalText(request.IconUrl);
        await SaveReferenceDataAsync("Kategorija s istim nazivom već postoji.", cancellationToken);
        return MapCategory(category);
    }

    public async Task DeleteCategoryAsync(int id, CancellationToken cancellationToken)
    {
        var category = await db.Categories.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Kategorija nije pronađena.");
        var inUse = await db.ProfessionalCategories.IgnoreQueryFilters()
                .AnyAsync(item => item.CategoryId == id, cancellationToken)
            || await db.JobPostings.IgnoreQueryFilters()
                .AnyAsync(item => item.CategoryId == id, cancellationToken)
            || await db.Reservations.IgnoreQueryFilters()
                .AnyAsync(item => item.CategoryId == id, cancellationToken);
        if (inUse)
        {
            throw new BusinessException("Kategoriju nije moguće obrisati dok je koriste profesionalci ili oglasi.");
        }

        db.Categories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ReservationStatusDefinitionResponse>> GetReservationStatusesAsync(
        CancellationToken cancellationToken)
    {
        return await db.ReservationStatusDefinitions
            .AsNoTracking()
            .OrderBy(status => status.Id)
            .Select(status => new ReservationStatusDefinitionResponse(
                (int)status.Id,
                status.Name,
                status.Description,
                status.IsActive))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ReservationStatusDefinitionResponse> CreateReservationStatusAsync(
        SaveReservationStatusDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var statusId = request.Id!.Value;
        var status = await db.ReservationStatusDefinitions
            .SingleOrDefaultAsync(item => item.Id == statusId, cancellationToken);
        if (status is { IsActive: true })
        {
            throw new BusinessException("Status rezervacije s ovom šifrom već postoji.");
        }

        if (status is null)
        {
            status = new ReservationStatusDefinition { Id = statusId };
            db.ReservationStatusDefinitions.Add(status);
        }

        status.Name = RequiredText(request.Name, "Naziv statusa");
        status.Description = RequiredText(request.Description, "Opis statusa");
        status.IsActive = true;
        await db.SaveChangesAsync(cancellationToken);
        return MapReservationStatus(status);
    }

    public async Task<ReservationStatusDefinitionResponse> UpdateReservationStatusAsync(
        int id,
        UpdateReservationStatusDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(typeof(ReservationStatus), id))
        {
            throw new NotFoundException("Status rezervacije nije pronađen.");
        }

        var statusId = (ReservationStatus)id;
        var status = await db.ReservationStatusDefinitions
            .SingleOrDefaultAsync(item => item.Id == statusId, cancellationToken)
            ?? throw new NotFoundException("Status rezervacije nije pronađen.");
        status.Name = RequiredText(request.Name, "Naziv statusa");
        status.Description = RequiredText(request.Description, "Opis statusa");
        await db.SaveChangesAsync(cancellationToken);
        return MapReservationStatus(status);
    }

    public async Task DeleteReservationStatusAsync(
        int id,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(typeof(ReservationStatus), id))
        {
            throw new NotFoundException("Status rezervacije nije pronađen.");
        }

        var statusId = (ReservationStatus)id;
        var status = await db.ReservationStatusDefinitions
            .SingleOrDefaultAsync(item => item.Id == statusId, cancellationToken)
            ?? throw new NotFoundException("Status rezervacije nije pronađen.");
        status.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<PagedResponse<TResponse>> PageAsync<TEntity, TResponse>(
        IOrderedQueryable<TEntity> query,
        PagedRequest request,
        System.Linq.Expressions.Expression<Func<TEntity, TResponse>> projection,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var page = paginationService.Normalize(request);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(projection)
            .ToArrayAsync(cancellationToken);
        return new PagedResponse<TResponse>(items, total, page.Page, page.PageSize);
    }

    private async Task SaveReferenceDataAsync(
        string duplicateMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new BusinessException(duplicateMessage);
        }
    }

    private static CategoryResponse MapCategory(Category category) => new(
        category.Id,
        category.Name,
        category.Description,
        category.IconUrl);

    private static ReservationStatusDefinitionResponse MapReservationStatus(
        ReservationStatusDefinition status) => new(
            (int)status.Id,
            status.Name,
            status.Description,
            status.IsActive);

    private static string RequiredText(string value, string fieldName)
    {
        var normalized = value.Trim();
        return normalized.Length > 0
            ? normalized
            : throw new BusinessException($"{fieldName} je obavezan.");
    }

    private static string? OptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
