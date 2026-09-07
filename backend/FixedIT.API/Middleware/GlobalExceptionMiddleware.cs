using FixedIT.API.CustomExceptions;
using FixedIT.API.DTOs.Common;

namespace FixedIT.API.Middleware;

public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
{
    private const string UnexpectedErrorMessage = "Dogodila se neočekivana greška na serveru.";

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            var (statusCode, message) = MapException(exception);
            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                logger.LogError(
                    exception,
                    "Unhandled exception while processing {Method} {Path}. TraceId: {TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.TraceIdentifier);
            }
            else
            {
                logger.LogWarning(
                    "Request {Method} {Path} failed with {StatusCode}. TraceId: {TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    statusCode,
                    context.TraceIdentifier);
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(
                new ErrorResponse(statusCode, message, context.TraceIdentifier));
        }
    }

    private static (int StatusCode, string Message) MapException(Exception exception)
    {
        return exception switch
        {
            BusinessException business => (business.StatusCode, business.Message),
            NotFoundException notFound => (notFound.StatusCode, notFound.Message),
            UnauthorizedException unauthorized => (unauthorized.StatusCode, unauthorized.Message),
            ServiceUnavailableException unavailable => (unavailable.StatusCode, unavailable.Message),
            _ => (StatusCodes.Status500InternalServerError, UnexpectedErrorMessage)
        };
    }
}
