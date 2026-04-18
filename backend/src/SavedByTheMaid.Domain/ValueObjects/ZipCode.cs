using System.Text.RegularExpressions;
using SavedByTheMaid.Domain.Common;

namespace SavedByTheMaid.Domain.ValueObjects;

/// <summary>
/// US ZIP code value object. Accepts 5-digit (`12345`) or ZIP+4 (`12345-6789`)
/// formats; storage normalizes to the 5-digit canonical form because the
/// service-area lookup table only keys on 5 digits.
///
/// Storage stays as <c>string</c> in entities (avoids a wide refactor).
/// Construct via <see cref="Create"/> in validators / services so the rule
/// lives in one place.
/// </summary>
public sealed class ZipCode : IEquatable<ZipCode>
{
    private static readonly Regex Pattern = new(
        @"^\d{5}(-\d{4})?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string Value { get; }

    private ZipCode(string value) => Value = value;

    /// <summary>Validates raw input (accepts 5-digit or ZIP+4).</summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return Pattern.IsMatch(value.Trim());
    }

    /// <summary>
    /// Returns the canonical 5-digit form regardless of input shape
    /// (`12345-6789` -> `12345`). Used to query <c>ServiceAreaZips</c>.
    /// </summary>
    public static string Normalize(string value)
    {
        var trimmed = value.Trim();
        var dashIdx = trimmed.IndexOf('-');
        return dashIdx > 0 ? trimmed[..dashIdx] : trimmed;
    }

    public static Result<ZipCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<ZipCode>.Failure(new Error("ZipCode.Empty", "ZIP code is required."));

        var trimmed = value.Trim();
        if (!Pattern.IsMatch(trimmed))
            return Result<ZipCode>.Failure(new Error("ZipCode.Invalid", "ZIP code must be 5 digits (or 5+4 format)."));

        return Result<ZipCode>.Success(new ZipCode(Normalize(trimmed)));
    }

    public static ZipCode From(string value)
    {
        var result = Create(value);
        return result.IsSuccess
            ? result.Value!
            : throw new ArgumentException(result.Error.Description, nameof(value));
    }

    public override string ToString() => Value;

    public bool Equals(ZipCode? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is ZipCode z && Equals(z);
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
    public static bool operator ==(ZipCode? a, ZipCode? b) => Equals(a, b);
    public static bool operator !=(ZipCode? a, ZipCode? b) => !Equals(a, b);

    public static implicit operator string(ZipCode zip) => zip.Value;
}
