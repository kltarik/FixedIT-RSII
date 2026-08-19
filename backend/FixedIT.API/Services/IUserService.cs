using FixedIT.API.DTOs.Users;
using Microsoft.AspNetCore.Http;

namespace FixedIT.API.Services;

public interface IUserService
{
    Task<UserProfileResponse> GetProfileAsync(string userId, CancellationToken cancellationToken);

    Task<UserProfileResponse> UpdateProfileAsync(
        string userId,
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken);

    Task<ProfilePictureResponse> UpdateProfilePictureAsync(
        string userId,
        IFormFile file,
        CancellationToken cancellationToken);
}
