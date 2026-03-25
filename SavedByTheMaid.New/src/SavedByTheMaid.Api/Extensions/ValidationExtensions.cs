using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace SavedByTheMaid.Api.Extensions;

/// <summary>
/// Extension methods for FluentValidation integration in controllers.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Validates the instance and returns a BadRequest result if validation fails, or null if valid.
    /// </summary>
    public static async Task<IActionResult?> ValidateAndReturnErrors<T>(
        this IValidator<T> validator, T instance)
    {
        var result = await validator.ValidateAsync(instance);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return new BadRequestObjectResult(new { message = "Validation failed", errors });
        }
        return null;
    }
}
