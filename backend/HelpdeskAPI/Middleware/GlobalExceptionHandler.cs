using System.Net;
using System.Text.Json;

namespace HelpdeskAPI.Middleware;

/// <summary>
/// Generic 500 mapper. Validation failures stay 400 via [ApiController];
/// anything unhandled becomes a stable JSON envelope, never a stack trace.
/// </summary>
public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled request failure {Method} {Path}", context.Request.Method, context.Request.Path);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = "Request failed. The team has been notified.",
                traceId = context.TraceIdentifier
            }));
        }
    }
}
