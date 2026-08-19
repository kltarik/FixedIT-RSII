using System.Security.Claims;
using FixedIT.API.CustomExceptions;

namespace FixedIT.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedException("Nedostaje identifikator prijavljenog korisnika.");
    }
}
