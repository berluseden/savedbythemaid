using System.Text.RegularExpressions;
using SavedByTheMaid.Domain.Common;

namespace SavedByTheMaid.Domain.ValueObjects;

/// <summary>
/// Email address value object. Owns the canonical validation + normalization
/// rules so the entire codebase agrees on what a valid email is.
///
/// Storage stays as <c>string</c> in entities (avoids a wide refactor). Use
/// <see cref="Create"/> at boundaries (validators, services) to construct
/// from raw input — it normalizes (trim, lowercase) and validates.
///
/// Equality is value-based; comparisons are case-insensitive after normalization.
/// </summary>
public sealed class Email : IEquatable<Email>
{
    public const int MaxLength = 254; // RFC 5321 limit

    // Pragmatic RFC 5322 regex — rejects obviously bad input without trying
    // to be a full parser. The mail server is the ultimate authority.
    private static readonly Regex Pattern = new(
        @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string Value { get; }

    private Email(string value) => Value = value;

    /// <summary>
    /// Validates raw input. Use this in FluentValidation rules so the
    /// canonical rule lives in one place.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var normalized = Normalize(value);
        return normalized.Length <= MaxLength && Pattern.IsMatch(normalized);
    }

    /// <summary>
    /// Normalizes for comparison/storage: trim + lowercase invariant.
    /// </summary>
    public static string Normalize(string value)
        => value.Trim().ToLowerInvariant();

    /// <summary>
    /// Result-returning factory — preferred for application services.
    /// </summary>
    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Email>.Failure(new Error("Email.Empty", "Email is required."));

        var normalized = Normalize(value);
        if (normalized.Length > MaxLength)
            return Result<Email>.Failure(new Error("Email.TooLong", $"Email must be at most {MaxLength} characters."));
        if (!Pattern.IsMatch(normalized))
            return Result<Email>.Failure(new Error("Email.Invalid", "Email format is invalid."));

        return Result<Email>.Success(new Email(normalized));
    }

    /// <summary>
    /// Throwing factory — convenient for tests and known-valid input.
    /// </summary>
    public static Email From(string value)
    {
        var result = Create(value);
        return result.IsSuccess
            ? result.Value!
            : throw new ArgumentException(result.Error.Description, nameof(value));
    }

    public override string ToString() => Value;

    public bool Equals(Email? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is Email e && Equals(e);
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
    public static bool operator ==(Email? a, Email? b) => Equals(a, b);
    public static bool operator !=(Email? a, Email? b) => !Equals(a, b);

    public static implicit operator string(Email email) => email.Value;
}
