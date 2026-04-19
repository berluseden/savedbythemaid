using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;

namespace SavedByTheMaid.Api.Middleware;

/// <summary>
/// Validates the XSRF-TOKEN double-submit cookie on state-changing requests
/// for authenticated users. Skips auth endpoints (login, register, refresh)
/// since the user has no token yet.
/// </summary>
public class CsrfValidationMiddleware
{
    private static readonly HashSet<string> _mutationMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    private static readonly HashSet<string> _skipPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/refresh",
        "/api/auth/forgot-password",
        "/api/auth/reset-password",
        "/api/antiforgery/token",
    };

    private readonly RequestDelegate _next;
    private readonly IAntiforgery _antiforgery;

    public CsrfValidationMiddleware(RequestDelegate next, IAntiforgery antiforgery)
    {
        _next = next;
        _antiforgery = antiforgery;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_mutationMethods.Contains(context.Request.Method) &&
            context.User.Identity?.IsAuthenticated == true &&
            !_skipPaths.Contains(context.Request.Path.Value ?? ""))
        {
            // Skip CSRF for endpoints decorated with [AllowAnonymous] — an authenticated
            // user hitting a public endpoint should not be blocked by CSRF (the endpoint
            // does not rely on the user's session to perform sensitive operations).
            var endpoint = context.GetEndpoint();
            var isAnonymous = endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null;

            if (!isAnonymous && !await _antiforgery.IsRequestValidAsync(context))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { message = "CSRF token validation failed." });
                return;
            }
        }

        await _next(context);
    }
}

public static class CsrfValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseCsrfValidation(this IApplicationBuilder app)
        => app.UseMiddleware<CsrfValidationMiddleware>();
}
