using System.Globalization;
using System.Reflection;
using FixedIT.API.Data;
using FixedIT.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace FixedIT.API.Filters;

public sealed class AuditLogActionFilter(
    IServiceScopeFactory serviceScopeFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuditLogActionFilter> logger) : IAsyncActionFilter, IOrderedFilter
{
    private static readonly HashSet<string> AuditedMethods = new(
        [HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete],
        StringComparer.OrdinalIgnoreCase);

    public int Order => -3000;

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var executedContext = await next();
        if (!ShouldAudit(executedContext))
        {
            return;
        }

        var httpContext = httpContextAccessor.HttpContext;
        var userId = httpContext?.User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var actionDescriptor = executedContext.ActionDescriptor as ControllerActionDescriptor;
            db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = executedContext.HttpContext.Request.Method.ToUpperInvariant(),
                EntityType = actionDescriptor?.ControllerName ?? "Nepoznato",
                EntityId = ResolveEntityId(executedContext),
                Details = $"HTTP status: {ResolveStatusCode(executedContext)}.",
                IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "nepoznato",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Could not persist audit log for {Method} {Path}.",
                executedContext.HttpContext.Request.Method,
                executedContext.HttpContext.Request.Path);
        }
    }

    private static bool ShouldAudit(ActionExecutedContext context)
    {
        return AuditedMethods.Contains(context.HttpContext.Request.Method)
            && context.HttpContext.User.Identity?.IsAuthenticated == true;
    }

    private static int ResolveStatusCode(ActionExecutedContext context)
    {
        var resultStatusCode = (context.Result as IStatusCodeActionResult)?.StatusCode;
        if (resultStatusCode.HasValue)
        {
            return resultStatusCode.Value;
        }

        if (context.Exception is not null)
        {
            return StatusCodes.Status500InternalServerError;
        }

        if (context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            return 499;
        }

        return context.HttpContext.Response.StatusCode;
    }

    private static string ResolveEntityId(ActionExecutedContext context)
    {
        var routeId = context.RouteData.Values
            .Where(pair => string.Equals(pair.Key, "id", StringComparison.OrdinalIgnoreCase)
                || pair.Key.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
            .Select(pair => Convert.ToString(pair.Value, CultureInfo.InvariantCulture))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        if (!string.IsNullOrWhiteSpace(routeId))
        {
            return routeId;
        }

        var resultValue = (context.Result as ObjectResult)?.Value;
        var idProperty = resultValue?.GetType().GetProperty(
            "Id",
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        return Convert.ToString(
                idProperty?.GetValue(resultValue),
                CultureInfo.InvariantCulture)
            ?? "nije dostupno";
    }
}
