using SavedByTheMaid.Domain.Common;

namespace SavedByTheMaid.Domain.Errors;

public static class ContactErrors
{
    public static readonly Error SendFailed = new(
        "Contact.SendFailed",
        "Failed to send the contact request. Please try again later.");

    public static readonly Error InvalidEmail = new(
        "Contact.InvalidEmail",
        "The provided email address is not valid.");

    public static readonly Error RateLimitExceeded = new(
        "Contact.RateLimitExceeded",
        "Too many contact requests. Please wait before trying again.");
}
