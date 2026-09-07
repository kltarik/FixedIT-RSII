using Microsoft.AspNetCore.Http;

namespace FixedIT.API.CustomExceptions;

public sealed class ServiceUnavailableException(string message) : Exception(message)
{
    public int StatusCode { get; } = StatusCodes.Status503ServiceUnavailable;
}
