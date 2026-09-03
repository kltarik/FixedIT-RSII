using FixedIT.API.Configuration;
using FixedIT.API.Constants;
using FixedIT.API.CustomExceptions;
using FixedIT.API.DTOs.Auth;
using FixedIT.API.Extensions;
using FixedIT.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FixedIT.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IAuthService authService,
    IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var response = await authService.RegisterAsync(request, cancellationToken);
        SetRefreshTokenCookie(response.RefreshToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);
        SetRefreshTokenCookie(response.RefreshToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Headers[AuthenticationConstants.RefreshTokenHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            Request.Cookies.TryGetValue(
                AuthenticationConstants.RefreshTokenCookieName,
                out refreshToken);
        }

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedException("Token za obnovu prijave je obavezan.");
        }

        var response = await authService.RefreshAsync(refreshToken, cancellationToken);
        SetRefreshTokenCookie(response.RefreshToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await authService.RequestPasswordResetAsync(request.Email, cancellationToken);
        return Ok(new ForgotPasswordResponse(
            "Ako nalog postoji, kod za promjenu lozinke je poslan na email."));
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await authService.ResetPasswordAsync(request, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(User.GetUserId(), cancellationToken);
        Response.Cookies.Delete(AuthenticationConstants.RefreshTokenCookieName);
        return NoContent();
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        Response.Cookies.Append(
            AuthenticationConstants.RefreshTokenCookieName,
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Path = "/api/auth",
                Expires = new DateTimeOffset(
                    DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays))
            });
    }
}
