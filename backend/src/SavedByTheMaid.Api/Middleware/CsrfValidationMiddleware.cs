using Microsoft.AspNetCore.Authorization;

namespace SavedByTheMaid.Api.Middleware;

/// <summary>
/// Validates CSRF via simple double-submit cookie: the XSRF-TOKEN cookie value must
/// equal the X-XSRF-TOKEN request header. Token is a random value generated per session
/// — not bound to user identity, so it survives login/logout without re-seeding issues.
/// </summary>
public class CsrfValidationMiddleware
{
    private const string CookieName = "XSRF-TOKEN";
    private const string HeaderName = "X-XSRF-TOKEN";

    private static readonly HashSet<string> _mutationMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    private static readonly HashSet<string> _skipPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/refresh",
        "/api/auth/forgot-password",
        "/api/auth/reset-password",
        "/api/auth/logout",
        "/api/antiforgery/token",
    };

    private readonly RequestDelegate _next;

    public CsrfValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_mutationMethods.Contains(context.Request.Method) &&
            context.User.Identity?.IsAuthenticated == true &&
            !_skipPaths.Contains(context.Request.Path.Value ?? ""))
        {
            var endpoint = context.GetEndpoint();
            var isAnonymous = endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null;

            if (!isAnonymous)
            {
                var cookieToken = context.Request.Cookies[CookieName];
                var headerToken = context.Request.Headers[HeaderName].ToString();

                if (string.IsNullOrEmpty(cookieToken) ||
                    string.IsNullOrEmpty(headerToken) ||
                    !string.Equals(cookieToken, headerToken, StringComparison.Ordinal))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new { message = "CSRF token validation failed." });
                    return;
                }
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
