namespace FixedIT.API.DTOs.Common;

public sealed record ErrorResponse(
    int StatusCode,
    string Message,
    string TraceId,
    string? StackTrace = null);
