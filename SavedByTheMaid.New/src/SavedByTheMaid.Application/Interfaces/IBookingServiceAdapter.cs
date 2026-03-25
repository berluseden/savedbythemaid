using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Application.Interfaces;

/// <summary>
/// Application-layer abstraction for the core booking service.
/// Wraps the Api layer's BookingService pricing and confirmation logic.
/// </summary>
public interface IBookingServiceAdapter
{
    /// <summary>
    /// Calculates price and time estimate for a service configuration.
    /// </summary>
    Task<PricingResultDto> CalculatePricingAsync(PricingInputDto input);

    /// <summary>
    /// Confirms a booking: validates soft reserve, creates user if needed,
    /// creates order + meeting, converts slots, and creates recurring meetings.
    /// </summary>
    Task<BookingConfirmationResultDto> ConfirmBookingAsync(ConfirmBookingInputDto input);
}

#region DTOs for Application-layer booking abstraction

public record PricingInputDto
{
    public int ServiceTypeId { get; init; }
    public List<RoomPricingItemDto>? Rooms { get; init; }
    public int Bedrooms { get; init; } = 1;
    public int Bathrooms { get; init; } = 1;
    public List<int>? AdditionalServiceIds { get; init; }
    public int? SquareFootage { get; init; }
    public DirtLevel DirtLevel { get; init; } = DirtLevel.Normal;
    public bool HasPets { get; init; }
    public bool HasElevator { get; init; } = true;
    public bool IsFirstTime { get; init; } = true;
    public RecurrenceType RecurrenceType { get; init; } = RecurrenceType.None;
}

public record RoomPricingItemDto(int RoomId, int Quantity);

public record PricingResultDto
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public int EstimatedMinutes { get; init; }
    public decimal Subtotal { get; init; }
    public decimal Discount { get; init; }
    public decimal Total { get; init; }
    public decimal DiscountPercent { get; init; }
}

public record ConfirmBookingInputDto
{
    public int SoftReserveId { get; init; }
    public string SessionId { get; init; } = "";
    public string? CustomerId { get; init; }
    public string ZipCode { get; init; } = "";
    public string Address { get; init; } = "";
    public string? AddressLine2 { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public int ServiceTypeId { get; init; }
    public int? CleaningPlaceId { get; init; }
    public int Bedrooms { get; init; } = 1;
    public int Bathrooms { get; init; } = 1;
    public int? SquareFootage { get; init; }
    public DirtLevel DirtLevel { get; init; }
    public bool HasPets { get; init; }
    public int? FloorLevel { get; init; }
    public bool HasElevator { get; init; } = true;
    public List<int>? AdditionalServiceIds { get; init; }
    public List<RoomPricingItemDto>? Rooms { get; init; }
    public decimal Total { get; init; }
    public RecurrenceType RecurrenceType { get; init; }
    public DateTime? RecurrenceEndDate { get; init; }
    public string? ContactName { get; init; }
    public string? ContactPhone { get; init; }
    public string ContactEmail { get; init; } = "";
    public string? Password { get; init; }
    public string? SpecialInstructions { get; init; }
}

public record BookingConfirmationResultDto
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public int OrderId { get; init; }
    public int MeetId { get; init; }
    public string ConfirmationNumber { get; init; } = "";
    public DateTime ScheduledStart { get; init; }
    public DateTime ScheduledEnd { get; init; }
    public decimal Total { get; init; }
    public string OrderStatus { get; init; } = "";
    public string Message { get; init; } = "";
    public AuthTokenResultDto? AuthToken { get; init; }
    public bool IsGuest { get; init; }
    public bool IsExpired { get; init; }
    public bool IsNotFound { get; init; }
    public bool IsAlreadyProcessed { get; init; }
}

public record AuthTokenResultDto
{
    public string AccessToken { get; init; } = "";
    public string RefreshToken { get; init; } = "";
    public DateTime ExpiresAt { get; init; }
    public bool IsNewUser { get; init; }
}

#endregion
