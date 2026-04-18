using SavedByTheMaid.Application.DTOs.Booking;
using SavedByTheMaid.Domain.Common;

namespace SavedByTheMaid.Application.Interfaces;

/// <summary>
/// Application service for the public booking wizard flow.
/// Handles coverage checks, estimates, availability, soft reserves, and booking confirmation.
/// </summary>
public interface IBookingApplicationService
{
    /// <summary>
    /// Checks whether a ZIP code is covered by any active service area.
    /// </summary>
    Task<Result<CoverageResponse>> CheckCoverageAsync(string zipCode);

    /// <summary>
    /// Calculates a price and time estimate for a given service configuration.
    /// </summary>
    Task<Result<EstimateResponse>> GetEstimateAsync(EstimateRequest request);

    /// <summary>
    /// Gets available time slots for a specific date and service area.
    /// </summary>
    Task<Result<AvailabilityResponse>> GetAvailabilityAsync(AvailabilityRequest request);

    /// <summary>
    /// Creates a temporary soft reserve (anti-collision) while the customer completes checkout.
    /// The reserve expires after a configurable TTL (default 15 minutes).
    /// </summary>
    Task<Result<SoftReserveResponse>> CreateSoftReserveAsync(CreateSoftReserveRequest request);

    /// <summary>
    /// Confirms a booking: validates the soft reserve, creates the user if needed,
    /// creates the order and meeting, converts soft reserve slots, and sets up recurring meetings.
    /// </summary>
    Task<Result<BookingConfirmationResponse>> ConfirmBookingAsync(ConfirmBookingRequest request);
}
