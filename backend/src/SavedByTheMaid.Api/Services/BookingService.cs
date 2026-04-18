using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Api.Auth;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Domain.Enums;
using SavedByTheMaid.Infrastructure.Data;

namespace SavedByTheMaid.Api.Services;

/// <summary>
/// Core booking business logic extracted from BookingController.
/// Handles pricing, confirmation, user creation, and recurring meetings.
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Calculates price and time estimate for a service configuration.
    /// Used by both Estimate and Confirm endpoints to ensure consistency.
    /// </summary>
    Task<PricingResult> CalculatePricingAsync(PricingInput input);

    /// <summary>
    /// Confirms a booking: validates soft reserve, creates user if needed,
    /// creates order + meeting, converts slots, and creates recurring meetings.
    /// </summary>
    Task<BookingConfirmationResult> ConfirmBookingAsync(ConfirmBookingInput input);

    /// <summary>
    /// Creates recurring meetings with slot acquisition for anti-collision.
    /// </summary>
    Task<int> CreateRecurringMeetingsAsync(ServiceOrder order, ServiceMeet firstMeet,
        RecurrenceType recurrenceType, DateTime? endDate);
}

#region Service Input/Output Models

public record PricingInput
{
    public int ServiceTypeId { get; init; }
    public List<RoomPricingItem>? Rooms { get; init; }
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

public record RoomPricingItem(int RoomId, int Quantity);

public record PricingResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public int EstimatedMinutes { get; init; }
    public decimal Subtotal { get; init; }
    public decimal Discount { get; init; }
    public decimal Total { get; init; }
    public decimal DiscountPercent { get; init; }
}

public record ConfirmBookingInput
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
    public List<RoomPricingItem>? Rooms { get; init; }
    public decimal Total { get; init; }
    public RecurrenceType RecurrenceType { get; init; }
    public DateTime? RecurrenceEndDate { get; init; }
    public string? ContactName { get; init; }
    public string? ContactPhone { get; init; }
    public string ContactEmail { get; init; } = "";
    public string? Password { get; init; }
    public string? SpecialInstructions { get; init; }
}

public record BookingConfirmationResult
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
    public AuthTokenResult? AuthToken { get; init; }
    public bool IsGuest { get; init; }
    public bool IsExpired { get; init; }
    public bool IsNotFound { get; init; }
    public bool IsAlreadyProcessed { get; init; }
}

public record AuthTokenResult
{
    public string AccessToken { get; init; } = "";
    public string RefreshToken { get; init; } = "";
    public DateTime ExpiresAt { get; init; }
    public bool IsNewUser { get; init; }
}

#endregion

public class BookingService : IBookingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BookingService> _logger;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly ISchedulingService _schedulingService;

    public BookingService(
        ApplicationDbContext context,
        ILogger<BookingService> logger,
        IJwtService jwtService,
        IPasswordHasher<ApplicationUser> passwordHasher,
        ISchedulingService schedulingService)
    {
        _context = context;
        _logger = logger;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _schedulingService = schedulingService;
    }

    public async Task<PricingResult> CalculatePricingAsync(PricingInput input)
    {
        var serviceType = await _context.ServiceTypes.FindAsync(input.ServiceTypeId);
        if (serviceType == null)
            return new PricingResult { Success = false, Error = "Invalid service type" };

        // Base price and time
        decimal subtotal = serviceType.Price;
        int totalMinutes = serviceType.EstimatedMinutes;

        // Room-based pricing (consistent between estimate and confirm)
        if (input.Rooms?.Any() == true)
        {
            foreach (var room in input.Rooms)
            {
                var roomType = await _context.CleaningPlaceRooms.FindAsync(room.RoomId);
                if (roomType != null)
                {
                    totalMinutes += roomType.BaseMinutes * room.Quantity;
                    subtotal += roomType.BasePrice * room.Quantity;
                }
            }
        }
        else
        {
            // Fallback: bedroom/bathroom pricing
            if (input.Bedrooms > 1)
            {
                subtotal += (input.Bedrooms - 1) * serviceType.PricePerBedroom;
                totalMinutes += (input.Bedrooms - 1) * serviceType.MinutesPerBedroom;
            }
            if (input.Bathrooms > 1)
            {
                subtotal += (input.Bathrooms - 1) * serviceType.PricePerBathroom;
                totalMinutes += (input.Bathrooms - 1) * serviceType.MinutesPerBathroom;
            }
        }

        // Additional services
        if (input.AdditionalServiceIds?.Any() == true)
        {
            var additionals = await _context.AdditionalServiceTypes
                .Where(a => input.AdditionalServiceIds.Contains(a.Id))
                .ToListAsync();

            subtotal += additionals.Sum(a => a.Price);
            totalMinutes += additionals.Sum(a => a.AdditionalMinutes);
        }

        // Apply multipliers
        var multipliers = await _context.PriceMultipliers
            .Where(m => m.IsActive)
            .Where(m => m.ServiceTypeId == null || m.ServiceTypeId == input.ServiceTypeId)
            .ToListAsync();

        decimal timeFactor = 1.0m;
        decimal priceFactor = 1.0m;

        foreach (var mult in multipliers)
        {
            bool applies = mult.ConditionType switch
            {
                MultiplierConditionType.SquareFootage when input.SquareFootage.HasValue =>
                    (!mult.MinValue.HasValue || input.SquareFootage >= mult.MinValue) &&
                    (!mult.MaxValue.HasValue || input.SquareFootage <= mult.MaxValue),
                MultiplierConditionType.DirtLevel =>
                    (int)input.DirtLevel == (int)(mult.MinValue ?? 1),
                MultiplierConditionType.HasPets => input.HasPets,
                MultiplierConditionType.FirstTime => input.IsFirstTime,
                MultiplierConditionType.NoElevator => !input.HasElevator,
                _ => false
            };

            if (applies)
            {
                if (mult.AppliesToTime) timeFactor *= mult.Factor;
                if (mult.AppliesToPrice) priceFactor *= mult.Factor;
            }
        }

        totalMinutes = (int)(totalMinutes * timeFactor);
        subtotal *= priceFactor;

        // Apply recurrence discount
        decimal discount = 0;
        if (input.RecurrenceType != RecurrenceType.None)
        {
            var recurrenceDiscount = await _context.RecurrenceDiscounts
                .FirstOrDefaultAsync(d => d.RecurrenceType == input.RecurrenceType && d.IsActive);
            if (recurrenceDiscount != null)
            {
                discount = subtotal * recurrenceDiscount.DiscountPercent;
            }
        }

        decimal total = subtotal - discount;

        return new PricingResult
        {
            Success = true,
            EstimatedMinutes = totalMinutes,
            Subtotal = subtotal,
            Discount = discount,
            Total = total,
            DiscountPercent = discount > 0 ? (discount / subtotal) * 100 : 0
        };
    }

    public async Task<BookingConfirmationResult> ConfirmBookingAsync(ConfirmBookingInput input)
    {
        // Validate soft reserve
        var softReserve = await _context.SoftReserves
            .FirstOrDefaultAsync(s => s.Id == input.SoftReserveId && s.SessionId == input.SessionId);

        if (softReserve == null)
            return new BookingConfirmationResult { Success = false, IsNotFound = true, Error = "Your time slot reservation was not found. Please go back and select a new time." };

        if (softReserve.Status != SoftReserveStatus.Active)
            return new BookingConfirmationResult { Success = false, IsAlreadyProcessed = true, Error = "This reservation has already been processed. Please start a new booking." };

        if (softReserve.ExpiresAt <= DateTime.UtcNow)
        {
            softReserve.Status = SoftReserveStatus.Expired;
            await _context.SaveChangesAsync();
            return new BookingConfirmationResult { Success = false, IsExpired = true, Error = "Your time slot has expired. Please go back and select a new time." };
        }

        // Recalculate pricing server-side (anti-fraud)
        _logger.LogInformation("Recalculating pricing for confirmation - SoftReserve {SoftReserveId}", input.SoftReserveId);

        var pricing = await CalculatePricingAsync(new PricingInput
        {
            ServiceTypeId = input.ServiceTypeId,
            Rooms = input.Rooms,
            Bedrooms = input.Bedrooms,
            Bathrooms = input.Bathrooms,
            AdditionalServiceIds = input.AdditionalServiceIds,
            SquareFootage = input.SquareFootage,
            DirtLevel = input.DirtLevel,
            HasPets = input.HasPets,
            HasElevator = input.HasElevator,
            RecurrenceType = input.RecurrenceType
        });

        if (!pricing.Success)
            return new BookingConfirmationResult { Success = false, Error = pricing.Error };

        // Log price mismatch but always use server-calculated price
        var tolerance = pricing.Total * 0.10m;
        if (Math.Abs(input.Total - pricing.Total) > tolerance)
        {
            _logger.LogWarning("Price mismatch detected - Expected: {Expected}, Received: {Received}", pricing.Total, input.Total);
        }

        // Create or find user
        var (customerId, authToken, isNewUser, isGuest) = await ResolveCustomerAsync(input);

        // Create order
        var order = new ServiceOrder
        {
            CustomerId = customerId,
            ServiceAreaId = softReserve.ServiceAreaId,
            ZipCode = input.ZipCode,
            Address = input.Address,
            AddressLine2 = input.AddressLine2,
            City = input.City,
            State = input.State,
            ServiceTypeId = input.ServiceTypeId,
            CleaningPlaceId = input.CleaningPlaceId,
            Bedrooms = input.Bedrooms,
            Bathrooms = input.Bathrooms,
            SquareFootage = input.SquareFootage,
            DirtLevel = input.DirtLevel,
            HasPets = input.HasPets,
            FloorLevel = input.FloorLevel,
            HasElevator = input.HasElevator,
            Subtotal = pricing.Subtotal,
            Tax = 0,
            Discount = pricing.Discount,
            Total = pricing.Total,
            OrderStatus = Domain.Enums.OrderStatus.PendingReview,
            RecurrenceType = input.RecurrenceType,
            RecurrenceEndDate = input.RecurrenceEndDate,
            Source = OrderSource.Website,
            ContactName = input.ContactName,
            ContactPhone = input.ContactPhone,
            ContactEmail = input.ContactEmail,
            SpecialInstructions = input.SpecialInstructions,
            PreferredStartTime = softReserve.ScheduledStart.TimeOfDay,
            EstimatedDurationMinutes = (int)(softReserve.ScheduledEnd - softReserve.ScheduledStart).TotalMinutes
        };

        _context.ServiceOrders.Add(order);
        await _context.SaveChangesAsync();

        // Save room selections
        if (input.Rooms?.Any() == true)
        {
            foreach (var room in input.Rooms)
            {
                var roomType = await _context.CleaningPlaceRooms.FindAsync(room.RoomId);
                if (roomType != null)
                {
                    _context.ServiceOrderRooms.Add(new ServiceOrderRoom
                    {
                        ServiceOrderId = order.Id,
                        CleaningPlaceRoomId = room.RoomId,
                        Quantity = room.Quantity,
                        CalculatedPrice = roomType.BasePrice * room.Quantity
                    });
                }
            }
        }

        // Save additional service items
        if (input.AdditionalServiceIds?.Any() == true)
        {
            foreach (var additionalId in input.AdditionalServiceIds)
            {
                var additional = await _context.AdditionalServiceTypes.FindAsync(additionalId);
                if (additional != null)
                {
                    _context.ServiceOrderItems.Add(new ServiceOrderItem
                    {
                        ServiceOrderId = order.Id,
                        AdditionalServiceTypeId = additionalId,
                        Description = additional.Title,
                        Quantity = 1,
                        UnitPrice = additional.Price,
                        Total = additional.Price
                    });
                }
            }
        }

        // Create meeting
        var meet = new ServiceMeet
        {
            ServiceOrderId = order.Id,
            AssignedEmployeeId = softReserve.EmployeeId,
            ServiceAreaId = softReserve.ServiceAreaId,
            ScheduledStart = softReserve.ScheduledStart,
            ScheduledEnd = softReserve.ScheduledEnd,
            EstimatedDurationMinutes = (int)(softReserve.ScheduledEnd - softReserve.ScheduledStart).TotalMinutes,
            Status = MeetStatus.Scheduled
        };

        _context.ServiceMeets.Add(meet);
        await _context.SaveChangesAsync();

        // Convert soft reserve to meeting
        softReserve.Status = SoftReserveStatus.Converted;
        softReserve.ServiceOrderId = order.Id;
        softReserve.CustomerId = customerId;

        await ConvertSoftReserveToMeetingOccupancyAsync(softReserve.Id, meet.Id);
        await _context.SaveChangesAsync();

        // Create recurring meetings if applicable
        if (input.RecurrenceType != RecurrenceType.None)
        {
            await CreateRecurringMeetingsAsync(order, meet, input.RecurrenceType, input.RecurrenceEndDate);
        }

        _logger.LogInformation("Order confirmed - OrderId: {OrderId}, MeetId: {MeetId}, Total: {Total}",
            order.Id, meet.Id, order.Total);

        return new BookingConfirmationResult
        {
            Success = true,
            OrderId = order.Id,
            MeetId = meet.Id,
            ConfirmationNumber = $"SBM-{order.Id:D6}",
            ScheduledStart = meet.ScheduledStart,
            ScheduledEnd = meet.ScheduledEnd,
            Total = order.Total,
            OrderStatus = order.OrderStatus.ToString(),
            Message = isGuest
                ? "Booking created. We'll send you a confirmation email."
                : isNewUser
                    ? "Account created! Your booking is pending confirmation. We'll notify you soon."
                    : "Booking received, pending confirmation. We'll contact you to confirm your appointment.",
            AuthToken = authToken,
            IsGuest = isGuest
        };
    }

    public async Task<int> CreateRecurringMeetingsAsync(ServiceOrder order, ServiceMeet firstMeet,
        RecurrenceType recurrenceType, DateTime? endDate)
    {
        var maxOccurrences = 8;
        var isMonthly = recurrenceType == RecurrenceType.Monthly;
        var dayInterval = recurrenceType switch
        {
            RecurrenceType.Weekly => 7,
            RecurrenceType.BiWeekly => 14,
            RecurrenceType.Monthly => 0,
            _ => 0
        };

        if (dayInterval == 0 && !isMonthly) return 0;

        var count = 0;
        var duration = firstMeet.EstimatedDurationMinutes;
        var currentStart = isMonthly
            ? firstMeet.ScheduledStart.AddMonths(1)
            : firstMeet.ScheduledStart.AddDays(dayInterval);
        var horizon = endDate ?? (isMonthly
            ? DateTime.UtcNow.AddMonths(maxOccurrences)
            : DateTime.UtcNow.AddDays(maxOccurrences * dayInterval));

        _logger.LogInformation("Creating recurring meetings for Order {OrderId}, type {RecurrenceType}",
            order.Id, recurrenceType);

        while (currentStart <= horizon && count < maxOccurrences)
        {
            var currentEnd = currentStart.AddMinutes(duration);

            // Validate employee works on that day
            var recurDayOfWeek = currentStart.DayOfWeek;
            var empSchedule = await _context.EmployeeSchedules
                .FirstOrDefaultAsync(s =>
                    s.EmployeeId == firstMeet.AssignedEmployeeId &&
                    s.DayOfWeek == recurDayOfWeek &&
                    s.IsAvailable && !s.IsDeleted);

            if (empSchedule == null ||
                currentStart.TimeOfDay < empSchedule.StartTime ||
                currentEnd.TimeOfDay > empSchedule.EndTime)
            {
                _logger.LogWarning("Skipping recurring meeting at {Date}: employee not scheduled", currentStart);
                currentStart = isMonthly ? currentStart.AddMonths(1) : currentStart.AddDays(dayInterval);
                continue;
            }

            // Check for conflicts with existing meetings
            var hasConflict = await _context.ServiceMeets
                .AnyAsync(m =>
                    m.AssignedEmployeeId == firstMeet.AssignedEmployeeId &&
                    m.Status != MeetStatus.Cancelled && m.Status != MeetStatus.NoShow &&
                    m.ScheduledStart < currentEnd && m.ScheduledEnd > currentStart);

            // Check for approved time off
            var hasTimeOff = await _context.EmployeeTimeOffs
                .AnyAsync(t =>
                    t.EmployeeId == firstMeet.AssignedEmployeeId &&
                    t.Status == TimeOffStatus.Approved &&
                    t.StartDateTime <= currentEnd &&
                    t.EndDateTime >= currentStart);

            if (!hasConflict && !hasTimeOff)
            {
                var recurringMeet = new ServiceMeet
                {
                    ServiceOrderId = order.Id,
                    AssignedEmployeeId = firstMeet.AssignedEmployeeId,
                    ServiceAreaId = firstMeet.ServiceAreaId,
                    ScheduledStart = currentStart,
                    ScheduledEnd = currentEnd,
                    EstimatedDurationMinutes = firstMeet.EstimatedDurationMinutes,
                    Status = MeetStatus.Scheduled
                };
                _context.ServiceMeets.Add(recurringMeet);
                await _context.SaveChangesAsync();

                // Acquire slots for anti-collision
                if (firstMeet.AssignedEmployeeId.HasValue)
                {
                    await _schedulingService.AcquireSlotsAsync(
                        firstMeet.AssignedEmployeeId.Value,
                        currentStart, currentEnd,
                        OccupancyType.Meeting, recurringMeet.Id);
                }
                count++;
            }
            else
            {
                _logger.LogWarning("Skipping recurring meeting at {Date} due to conflict or time off", currentStart);
            }

            currentStart = isMonthly ? currentStart.AddMonths(1) : currentStart.AddDays(dayInterval);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Created {Count} recurring meetings with slots for Order {OrderId}", count, order.Id);
        return count;
    }

    /// <summary>
    /// Resolves or creates the customer for a booking.
    /// Returns (customerId, authToken, isNewUser, isGuest).
    /// </summary>
    private async Task<(string? CustomerId, AuthTokenResult? AuthToken, bool IsNewUser, bool IsGuest)>
        ResolveCustomerAsync(ConfirmBookingInput input)
    {
        if (!string.IsNullOrEmpty(input.CustomerId))
            return (input.CustomerId, null, false, false);

        // Check if email already exists
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == input.ContactEmail);

        if (existingUser != null)
        {
            _logger.LogInformation("Booking created for existing user (not logged in): {Email}", input.ContactEmail);
            return (existingUser.Id, null, false, false);
        }

        if (string.IsNullOrWhiteSpace(input.Password))
        {
            _logger.LogInformation("Guest booking created without account: {Email}", input.ContactEmail);
            return (null, null, false, true);
        }

        // Create new user
        var newUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = input.ContactEmail,
            NormalizedUserName = input.ContactEmail.ToUpperInvariant(),
            Email = input.ContactEmail,
            NormalizedEmail = input.ContactEmail.ToUpperInvariant(),
            EmailConfirmed = false,
            PhoneNumber = input.ContactPhone,
            SecurityStamp = Guid.NewGuid().ToString(),
            FirstName = input.ContactName?.Split(' ').FirstOrDefault(),
            LastName = input.ContactName?.Split(' ').Skip(1).FirstOrDefault()
        };

        newUser.PasswordHash = _passwordHasher.HashPassword(newUser, input.Password);
        _context.Users.Add(newUser);

        var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == Roles.Customer);
        if (customerRole != null)
        {
            _context.UserRoles.Add(new IdentityUserRole<string>
            {
                UserId = newUser.Id,
                RoleId = customerRole.Id
            });
        }

        var accessToken = _jwtService.GenerateAccessToken(newUser.Id, newUser.Email!, new[] { Roles.Customer });
        var refreshToken = _jwtService.GenerateRefreshToken();

        _logger.LogInformation("User created automatically during booking: {Email}", input.ContactEmail);

        return (newUser.Id, new AuthTokenResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsNewUser = true
        }, true, false);
    }

    /// <summary>
    /// Converts SlotOccupancy entries from SoftReserve to Meeting type.
    /// </summary>
    private async Task ConvertSoftReserveToMeetingOccupancyAsync(int softReserveId, int meetId)
    {
        var now = DateTime.UtcNow;
        var slots = await _context.SlotOccupancies
            .Where(s => s.OccupancyType == OccupancyType.SoftReserve && s.ReferenceId == softReserveId)
            .ToListAsync();

        foreach (var slot in slots)
        {
            slot.OccupancyType = OccupancyType.Meeting;
            slot.ReferenceId = meetId;
            slot.ExpiresAt = null;
            slot.UpdatedAt = now;
        }
    }
}
