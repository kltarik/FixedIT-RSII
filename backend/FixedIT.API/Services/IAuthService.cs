using FixedIT.API.DTOs.Auth;

namespace FixedIT.API.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task LogoutAsync(string userId, CancellationToken cancellationToken);
    Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
}
