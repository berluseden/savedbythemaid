using FluentValidation;
using SavedByTheMaid.Domain.ValueObjects;

namespace SavedByTheMaid.Application.Validators;

/// <summary>
/// FluentValidation extensions that delegate to the canonical Value Object
/// rules in <see cref="Email"/> and <see cref="ZipCode"/>. Validators
/// across the codebase should use these so that adding/changing a rule
/// (e.g. allow international emails) happens in one place — the VO —
/// instead of scattered regex literals.
/// </summary>
public static class ValueObjectRules
{
    /// <summary>
    /// Replaces FluentValidation's built-in <c>EmailAddress()</c> with
    /// <see cref="Email.IsValid"/>. Same call shape:
    /// <c>RuleFor(x =&gt; x.Email).IsValidEmail();</c>
    /// </summary>
    public static IRuleBuilderOptions<T, string?> IsValidEmail<T>(
        this IRuleBuilder<T, string?> rule)
    {
        return rule
            .Must(value => Email.IsValid(value))
            .WithMessage("'{PropertyName}' is not a valid email address.");
    }

    /// <summary>
    /// US ZIP code (5-digit or 5+4). Single source of truth in
    /// <see cref="ZipCode.IsValid"/>.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> IsValidZipCode<T>(
        this IRuleBuilder<T, string?> rule)
    {
        return rule
            .Must(value => ZipCode.IsValid(value))
            .WithMessage("'{PropertyName}' must be a 5-digit ZIP code (or 5+4 format).");
    }
}
