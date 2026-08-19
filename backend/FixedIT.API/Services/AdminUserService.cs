using FixedIT.API.CustomExceptions;
using FixedIT.API.Data;
using FixedIT.API.DTOs.Admin;
using FixedIT.API.DTOs.Common;
using FixedIT.API.Localization;
using FixedIT.API.Models;
using FixedIT.API.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FixedIT.API.Services;

public sealed class AdminUserService(
    AppDbContext db,
    UserManager<User> userManager,
    IPaginationService paginationService,
    IFileUploadService fileUploadService,
    ILogger<AdminUserService> logger) : IAdminUserService
{
    public async Task<PagedResponse<AdminUserResponse>> GetPageAsync(
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        var page = paginationService.Normalize(request);
        var query = db.Users.IgnoreQueryFilters().AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .ThenBy(user => user.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(user => new AdminUserPageItem(
                user.Id,
                user.Email ?? string.Empty,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.CityId,
                user.City.Name,
                user.IsActive,
                user.CreatedAt))
            .ToListAsync(cancellationToken);
        var userIds = users.Select(user => user.Id).ToArray();
        var roleRows = await db.UserRoles
            .Where(link => userIds.Contains(link.UserId))
            .Join(
                db.Roles,
                link => link.RoleId,
                role => role.Id,
                (link, role) => new { link.UserId, RoleName = role.Name! })
            .ToListAsync(cancellationToken);
        var rolesByUser = roleRows
            .GroupBy(row => row.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<string>)group
                    .Select(row => row.RoleName)
                    .OrderBy(name => name)
                    .ToArray());
        var items = users
            .Select(user => MapUser(
                user,
                rolesByUser.GetValueOrDefault(user.Id) ?? []))
            .ToArray();

        return new PagedResponse<AdminUserResponse>(
            items,
            total,
            page.Page,
            page.PageSize);
    }

    public async Task<AdminUserResponse> SetActiveAsync(
        string adminUserId,
        string userId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        if (!isActive && userId == adminUserId)
        {
            throw new BusinessException("Administrator ne može deaktivirati vlastiti nalog.");
        }

        var user = await db.Users
            .IgnoreQueryFilters()
            .Include(item => item.City)
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Korisnik nije pronađen.");
        user.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
        var roles = await userManager.GetRolesAsync(user);

        return new AdminUserResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.CityId,
            user.City.Name,
            user.IsActive,
            user.CreatedAt,
            roles.ToArray());
    }

    public async Task DeleteAsync(
        string adminUserId,
        string userId,
        CancellationToken cancellationToken)
    {
        if (userId == adminUserId)
        {
            throw new BusinessException("Administrator ne može obrisati vlastiti nalog.");
        }

        var user = await db.Users
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Korisnik nije pronađen.");
        var professionalId = await db.ProfessionalProfiles
            .IgnoreQueryFilters()
            .Where(profile => profile.UserId == userId)
            .Select(profile => (int?)profile.Id)
            .SingleOrDefaultAsync(cancellationToken);
        var activeStatuses = new[]
        {
            ReservationStatus.Pending,
            ReservationStatus.Accepted,
            ReservationStatus.InProgress
        };
        var hasActiveReservations = await db.Reservations
            .IgnoreQueryFilters()
            .AnyAsync(
                reservation => activeStatuses.Contains(reservation.Status)
                    && (reservation.ClientUserId == userId
                        || (professionalId.HasValue
                            && reservation.ProfessionalProfileId == professionalId.Value)),
                cancellationToken);
        if (hasActiveReservations)
        {
            throw new BusinessException("Korisnik se ne može obrisati dok postoje aktivne rezervacije.");
        }

        if (await HasRestrictDeleteRelationsAsync(userId, professionalId, cancellationToken))
        {
            throw new BusinessException("Korisnik se ne može obrisati dok postoje vezani poslovni podaci.");
        }

        var imageUrls = new List<string>();
        if (!string.IsNullOrWhiteSpace(user.ProfilePictureUrl))
        {
            imageUrls.Add(user.ProfilePictureUrl);
        }

        if (professionalId.HasValue)
        {
            imageUrls.AddRange(await db.PortfolioItems
                .IgnoreQueryFilters()
                .Where(item => item.ProfessionalProfileId == professionalId.Value)
                .Select(item => item.ImageUrl)
                .ToListAsync(cancellationToken));
        }

        EnsureSucceeded(await userManager.DeleteAsync(user));
        foreach (var imageUrl in imageUrls)
        {
            try
            {
                await fileUploadService.DeleteImageAsync(imageUrl);
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "User {UserId} was deleted, but stored image {ImageUrl} could not be removed.",
                    userId,
                    imageUrl);
            }
        }
    }

    private async Task<bool> HasRestrictDeleteRelationsAsync(
        string userId,
        int? professionalId,
        CancellationToken cancellationToken)
    {
        if (await db.JobPostings.IgnoreQueryFilters()
                .AnyAsync(item => item.ClientUserId == userId, cancellationToken)
            || await db.Reservations.IgnoreQueryFilters()
                .AnyAsync(item => item.ClientUserId == userId, cancellationToken)
            || await db.Reviews.IgnoreQueryFilters()
                .AnyAsync(item => item.ClientUserId == userId, cancellationToken)
            || await db.ConversationParticipants.IgnoreQueryFilters()
                .AnyAsync(item => item.UserId == userId, cancellationToken)
            || await db.Messages.IgnoreQueryFilters()
                .AnyAsync(item => item.SenderUserId == userId, cancellationToken)
            || await db.AuditLogs.IgnoreQueryFilters()
                .AnyAsync(item => item.UserId == userId, cancellationToken)
            || await db.UserRatings.IgnoreQueryFilters()
                .AnyAsync(item => item.UserId == userId, cancellationToken))
        {
            return true;
        }

        if (!professionalId.HasValue)
        {
            return false;
        }

        return await db.JobOffers.IgnoreQueryFilters()
                .AnyAsync(item => item.ProfessionalProfileId == professionalId.Value, cancellationToken)
            || await db.Reservations.IgnoreQueryFilters()
                .AnyAsync(item => item.ProfessionalProfileId == professionalId.Value, cancellationToken)
            || await db.Reviews.IgnoreQueryFilters()
                .AnyAsync(item => item.ProfessionalProfileId == professionalId.Value, cancellationToken)
            || await db.UserRatings.IgnoreQueryFilters()
                .AnyAsync(item => item.ProfessionalProfileId == professionalId.Value, cancellationToken);
    }

    private static AdminUserResponse MapUser(
        AdminUserPageItem user,
        IReadOnlyCollection<string> roles)
    {
        return new AdminUserResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.CityId,
            user.CityName,
            user.IsActive,
            user.CreatedAt,
            roles);
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        throw new BusinessException(BosnianIdentityErrors.Build(result));
    }

    private sealed record AdminUserPageItem(
        string Id,
        string Email,
        string FirstName,
        string LastName,
        string? PhoneNumber,
        int CityId,
        string CityName,
        bool IsActive,
        DateTime CreatedAt);
}
