using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SavedByTheMaid.Api.Middleware;

/// <summary>
/// RFC 9457-compliant global exception handler using the ASP.NET Core IExceptionHandler
/// pattern. Registered via AddExceptionHandler&lt;GlobalExceptionHandler&gt;() and
/// activated by app.UseExceptionHandler().
/// Stack traces are only included in Development to avoid information disclosure.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString();

        _logger.LogError(
            exception,
            "Unhandled exception. CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}",
            correlationId,
            httpContext.Request.Path,
            httpContext.Request.Method);

        var (statusCode, title) = exception switch
        {
            ValidationException   => (StatusCodes.Status400BadRequest,  "Validation Error"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            KeyNotFoundException  => (StatusCodes.Status404NotFound,    "Resource Not Found"),
            InvalidOperationException => (StatusCodes.Status400BadRequest, "Bad Request"),
            _                     => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        var problem = new ProblemDetails
        {
            Type     = $"https://httpstatuses.io/{statusCode}",
            Title    = title,
            Status   = statusCode,
            Detail   = _env.IsDevelopment() ? exception.Message : "An unexpected error occurred. Please try again later.",
            Instance = httpContext.Request.Path
        };

        problem.Extensions["correlationId"] = correlationId;

        // Validation errors: include the list of error messages
        if (exception is ValidationException validationEx)
        {
            problem.Detail = "One or more validation errors occurred.";
            problem.Extensions["errors"] = validationEx.Errors;
        }

        // Stack trace only in Development — never in Production/Staging
        if (_env.IsDevelopment())
        {
            problem.Extensions["stackTrace"] = exception.StackTrace;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}
