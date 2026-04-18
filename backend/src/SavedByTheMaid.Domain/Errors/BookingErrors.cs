using SavedByTheMaid.Domain.Common;

namespace SavedByTheMaid.Domain.Errors;

public static class BookingErrors
{
    public static readonly Error NotFound = new(
        "Booking.NotFound",
        "The booking was not found.");

    public static readonly Error InvalidSoftReserve = new(
        "Booking.InvalidSoftReserve",
        "The soft reserve is invalid or has already been processed.");

    public static readonly Error SoftReserveExpired = new(
        "Booking.SoftReserveExpired",
        "The time slot reservation has expired. Please go back and select a new time.");

    public static readonly Error PricingMismatch = new(
        "Booking.PricingMismatch",
        "The submitted price does not match the server-calculated price.");

    public static readonly Error SlotUnavailable = new(
        "Booking.SlotUnavailable",
        "The requested time slot is no longer available.");

    public static Error SoftReserveNotFound(int softReserveId) => new(
        "Booking.SoftReserveNotFound",
        $"Soft reserve with ID {softReserveId} was not found.");

    public static Error InvalidServiceType(int serviceTypeId) => new(
        "Booking.InvalidServiceType",
        $"Service type with ID {serviceTypeId} is invalid or does not exist.");

    public static Error ZipCodeNotCovered(string zipCode) => new(
        "Booking.ZipCodeNotCovered",
        $"ZIP code {zipCode} is not covered by any service area.");
}
