using SavedByTheMaid.Domain.Common;

namespace SavedByTheMaid.Domain.Errors;

public static class AuthErrors
{
    public static readonly Error InvalidCredentials = new(
        "Auth.InvalidCredentials",
        "Invalid email or password.");

    public static readonly Error EmailAlreadyExists = new(
        "Auth.EmailAlreadyExists",
        "An account with this email address already exists.");

    public static readonly Error UserNotFound = new(
        "Auth.UserNotFound",
        "No account was found with the provided information.");

    public static readonly Error InvalidResetToken = new(
        "Auth.InvalidResetToken",
        "The password reset token is invalid or has expired.");

    public static readonly Error WeakPassword = new(
        "Auth.WeakPassword",
        "The password does not meet the minimum security requirements.");

    public static readonly Error Unauthorized = new(
        "Auth.Unauthorized",
        "You are not authorized to perform this action.");

    public static Error EmailNotFound(string email) => new(
        "Auth.EmailNotFound",
        $"No account was found with the email {email}.");
}
