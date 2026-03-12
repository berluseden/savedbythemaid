using System.Net;
using System.Text.Json;

namespace SavedByTheMaid.Api.Middleware;

/// <summary>
/// Global exception handling middleware
/// Catches all unhandled exceptions and returns consistent JSON responses
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = Guid.NewGuid().ToString();
        
        _logger.LogError(
            exception,
            "Unhandled error. CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}",
            correlationId,
            context.Request.Path,
            context.Request.Method);

        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            ValidationException validationEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Message = "Validation error",
                Errors = validationEx.Errors,
                CorrelationId = correlationId
            },
            
            UnauthorizedAccessException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Message = "Unauthorized",
                CorrelationId = correlationId
            },
            
            KeyNotFoundException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                Message = "Resource not found",
                CorrelationId = correlationId
            },
            
            InvalidOperationException invalidOpEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Message = invalidOpEx.Message,
                CorrelationId = correlationId
            },
            
            _ => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "An internal error has occurred. Please contact support.",
                CorrelationId = correlationId
            }
        };

        // In development, include error details
        if (_environment.IsDevelopment())
        {
            response.Details = exception.Message;
            response.StackTrace = exception.StackTrace;
        }

        context.Response.StatusCode = response.StatusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}

/// <summary>
/// Standardized error response
/// </summary>
public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = "";
    public string CorrelationId { get; set; } = "";
    public string[]? Errors { get; set; }
    public string? Details { get; set; }
    public string? StackTrace { get; set; }
}

/// <summary>
/// Custom exception for validation errors
/// </summary>
public class ValidationException : Exception
{
    public string[] Errors { get; }

    public ValidationException(string[] errors) : base("Validation error")
    {
        Errors = errors;
    }

    public ValidationException(string error) : base(error)
    {
        Errors = new[] { error };
    }
}

/// <summary>
/// Extension method to add the middleware
/// </summary>
public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
