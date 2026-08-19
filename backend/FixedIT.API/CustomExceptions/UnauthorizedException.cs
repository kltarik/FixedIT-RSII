using Microsoft.AspNetCore.Http;

namespace FixedIT.API.CustomExceptions;

public class UnauthorizedException(string message) : Exception(message)
{
    public int StatusCode { get; } = StatusCodes.Status401Unauthorized;
}
