using FixedIT.API.Constants;
using FixedIT.API.CustomExceptions;
using FixedIT.API.Data;
using FixedIT.API.DTOs.Users;
using FixedIT.API.Localization;
using FixedIT.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FixedIT.API.Services;

public sealed class UserService(
    AppDbContext db,
    UserManager<User> userManager,
    IFileUploadService fileUploadService) : IUserService
{
    public async Task<UserProfileResponse> GetProfileAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var user = await db.Users
            .AsNoTracking()
            .Include(item => item.City)
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Korisnički profil nije pronađen.");

        return await MapProfileAsync(user);
    }

    public async Task<UserProfileResponse> UpdateProfileAsync(
        string userId,
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(item => item.City)
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Korisnički profil nije pronađen.");
        var city = await db.Cities
            .SingleOrDefaultAsync(item => item.Id == request.CityId, cancellationToken)
            ?? throw new NotFoundException("Odabrani grad nije pronađen.");

        var email = request.Email.Trim();
        var normalizedEmail = userManager.NormalizeEmail(email);
        var emailBelongsToAnotherUser = await db.Users
            .IgnoreQueryFilters()
            .AnyAsync(
                item => item.NormalizedEmail == normalizedEmail && item.Id != user.Id,
                cancellationToken);
        if (emailBelongsToAnotherUser)
        {
            throw new BusinessException("Nalog s ovom email adresom već postoji.");
        }

        user.Email = email;
        user.UserName = email;
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? null
            : request.PhoneNumber.Trim();
        user.CityId = city.Id;
        user.City = city;

        EnsureSucceeded(await userManager.UpdateAsync(user));
        return await MapProfileAsync(user);
    }

    public async Task<ProfilePictureResponse> UpdateProfilePictureAsync(
        string userId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(
            item => item.Id == userId,
            cancellationToken)
            ?? throw new NotFoundException("Korisnički profil nije pronađen.");
        var oldPictureUrl = user.ProfilePictureUrl;
        var storedFile = await fileUploadService.SaveImageAsync(
            file,
            FileStorageConstants.ProfilePicturesFolder,
            cancellationToken);

        user.ProfilePictureUrl = storedFile.PublicUrl;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            await fileUploadService.DeleteImageAsync(storedFile.PublicUrl);
            EnsureSucceeded(updateResult);
        }

        await fileUploadService.DeleteImageAsync(oldPictureUrl);
        return new ProfilePictureResponse(storedFile.PublicUrl);
    }

    private async Task<UserProfileResponse> MapProfileAsync(User user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new UserProfileResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.ProfilePictureUrl,
            user.CityId,
            user.City.Name,
            user.IsActive,
            roles.ToArray());
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        throw new BusinessException(BosnianIdentityErrors.Build(result));
    }
}
