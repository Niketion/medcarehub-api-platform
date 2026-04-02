using MedCareHub.Api.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace MedCareHub.Api.Middleware;

/// <summary>
/// Converts application exceptions into RFC 7807 problem details responses.
/// </summary>
/// <remarks>
/// This middleware centralizes the HTTP mapping of custom domain/application exceptions
/// so that controllers and services can throw typed exceptions without duplicating response logic.
/// </remarks>
public sealed class ApiExceptionMiddleware : IMiddleware
{
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="ApiExceptionMiddleware"/>.
    /// </summary>
    public ApiExceptionMiddleware(ILogger<ApiExceptionMiddleware> logger) => _logger = logger;

    /// <summary>
    /// Executes the middleware pipeline and translates known exceptions to problem details.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (ApiException ex)
        {
            _logger.LogWarning(ex, "API exception: {Title} ({StatusCode})", ex.Title, ex.StatusCode);
            await WriteProblemAsync(context, ex.StatusCode, ex.Title, ex.Message, ex.Extra);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "UnauthorizedAccessException");
            await WriteProblemAsync(context, StatusCodes.Status403Forbidden, "Forbidden", ex.Message, null);
        }
    }

    private static async Task WriteProblemAsync(HttpContext ctx, int status, string title, string detail, object? extra)
    {
        if (ctx.Response.HasStarted)
            return;

        ctx.Response.Clear();
        ctx.Response.StatusCode = status;

        var pd = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = ctx.Request.Path
        };

        if (extra is not null)
            pd.Extensions["extra"] = extra;

        ctx.Response.ContentType = "application/problem+json";
        await ctx.Response.WriteAsJsonAsync(pd);
    }
}