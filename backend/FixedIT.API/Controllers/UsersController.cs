using FixedIT.API.DTOs.Users;
using FixedIT.API.Extensions;
using FixedIT.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixedIT.API.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileResponse>> GetProfile(
        CancellationToken cancellationToken)
    {
        var response = await userService.GetProfileAsync(
            User.GetUserId(),
            cancellationToken);
        return Ok(response);
    }

    [HttpPut("profile")]
    public async Task<ActionResult<UserProfileResponse>> UpdateProfile(
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken)
    {
        var response = await userService.UpdateProfileAsync(
            User.GetUserId(),
            request,
            cancellationToken);
        return Ok(response);
    }

    [HttpPost("profile/picture")]
    public async Task<ActionResult<ProfilePictureResponse>> UpdateProfilePicture(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        var response = await userService.UpdateProfilePictureAsync(
            User.GetUserId(),
            file,
            cancellationToken);
        return Ok(response);
    }
}
