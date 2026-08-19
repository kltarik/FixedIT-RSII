using Microsoft.AspNetCore.Http;

namespace FixedIT.API.CustomExceptions;

public class BusinessException(string message) : Exception(message)
{
    public int StatusCode { get; } = StatusCodes.Status400BadRequest;
}
