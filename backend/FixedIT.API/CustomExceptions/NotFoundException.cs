using Microsoft.AspNetCore.Http;

namespace FixedIT.API.CustomExceptions;

public class NotFoundException(string message) : Exception(message)
{
    public int StatusCode { get; } = StatusCodes.Status404NotFound;
}
