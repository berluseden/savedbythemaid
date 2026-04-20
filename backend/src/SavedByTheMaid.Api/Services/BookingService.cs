using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Api.Auth;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Domain.Enums;
using SavedByTheMaid.Infrastructure.Data;

namespace SavedByTheMaid.Api.Services;

/// <summary>
/// Core booking business logic extracted from BookingController.
/// Handles pricing, confirmation, and user creation.
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Calculates price and time estimate for a service configuration.
    /// Used by both Estimate and Confirm endpoints to ensure consistency.
    /// </summary>
    Task<PricingResult> CalculatePricingAsync(PricingInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a booking: validates soft reserve, creates user if needed,
    /// creates order + meeting, and converts slots.
    /// </summary>
    Task<BookingConfirmationResult> ConfirmBookingAsync(ConfirmBookingInput input, CancellationToken cancellationToken = default);
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
}

public record RoomPricingItem(int RoomId, int Quantity);

public record PriceLineItem(string Label, decimal Amount);

public record PricingResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public int EstimatedMinutes { get; init; }
    public decimal Subtotal { get; init; }
    public decimal Discount { get; init; }
    public decimal Total { get; init; }
    public decimal DiscountPercent { get; init; }
    public List<PriceLineItem> LineItems { get; init; } = [];
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
    public bool IsFirstTime { get; init; }
    public List<int>? AdditionalServiceIds { get; init; }
    public List<RoomPricingItem>? Rooms { get; init; }
    public decimal Total { get; init; }
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

    public async Task<PricingResult> CalculatePricingAsync(PricingInput input, CancellationToken cancellationToken = default)
    {
        var serviceType = await _context.ServiceTypes.FindAsync(new object[] { input.ServiceTypeId }, cancellationToken);
        if (serviceType == null)
            return new PricingResult { Success = false, Error = "Invalid service type" };

        // Base price and time
        decimal subtotal = serviceType.Price;
        int totalMinutes = serviceType.EstimatedMinutes;
        var lineItems = new List<PriceLineItem>();
        lineItems.Add(new PriceLineItem(serviceType.Name, serviceType.Price));

        // Room-based pricing (consistent between estimate and confirm)
        if (input.Rooms?.Any() == true)
        {
            // Batch-load all room types in a single query to avoid N+1
            var roomIds = input.Rooms.Select(r => r.RoomId).ToList();
            var roomTypes = await _context.CleaningPlaceRooms
                .AsNoTracking()
                .Where(r => roomIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, cancellationToken);

            foreach (var room in input.Rooms)
            {
                if (roomTypes.TryGetValue(room.RoomId, out var roomType))
                {
                    var roomAmount = roomType.BasePrice * room.Quantity;
                    totalMinutes += roomType.BaseMinutes * room.Quantity;
                    subtotal += roomAmount;
                    var roomLabel = room.Quantity > 1 ? $"{roomType.Name} ×{room.Quantity}" : roomType.Name;
                    lineItems.Add(new PriceLineItem(roomLabel, roomAmount));
                }
            }
        }
        else
        {
            // Fallback: bedroom/bathroom pricing
            if (input.Bedrooms > 1)
            {
                var extraBedrooms = input.Bedrooms - 1;
                var bedroomAmount = extraBedrooms * serviceType.PricePerBedroom;
                subtotal += bedroomAmount;
                totalMinutes += extraBedrooms * serviceType.MinutesPerBedroom;
                var bedroomLabel = extraBedrooms == 1 ? "Extra bedroom" : $"Extra bedrooms ×{extraBedrooms}";
                lineItems.Add(new PriceLineItem(bedroomLabel, bedroomAmount));
            }
            if (input.Bathrooms > 1)
            {
                var extraBathrooms = input.Bathrooms - 1;
                var bathroomAmount = extraBathrooms * serviceType.PricePerBathroom;
                subtotal += bathroomAmount;
                totalMinutes += extraBathrooms * serviceType.MinutesPerBathroom;
                var bathroomLabel = extraBathrooms == 1 ? "Extra bathroom" : $"Extra bathrooms ×{extraBathrooms}";
                lineItems.Add(new PriceLineItem(bathroomLabel, bathroomAmount));
            }
        }

        // Additional services
        if (input.AdditionalServiceIds?.Any() == true)
        {
            var additionals = await _context.AdditionalServiceTypes
                .Where(a => input.AdditionalServiceIds.Contains(a.Id))
                .ToListAsync(cancellationToken);

            foreach (var addon in additionals)
            {
                subtotal += addon.Price;
                totalMinutes += addon.AdditionalMinutes;
                lineItems.Add(new PriceLineItem(addon.Title, addon.Price));
            }
        }

        // Apply multipliers
        var multipliers = await _context.PriceMultipliers
            .Where(m => m.IsActive)
            .Where(m => m.ServiceTypeId == null || m.ServiceTypeId == input.ServiceTypeId)
            .ToListAsync(cancellationToken);

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
        var subtotalBeforeMultiplier = subtotal;
        subtotal *= priceFactor;

        if (priceFactor != 1.0m)
        {
            var modifierAmount = subtotal - subtotalBeforeMultiplier;
            lineItems.Add(new PriceLineItem("Adjustments", modifierAmount));
        }

        decimal discount = 0;
        decimal total = subtotal - discount;

        return new PricingResult
        {
            Success = true,
            EstimatedMinutes = totalMinutes,
            Subtotal = subtotal,
            Discount = discount,
            Total = total,
            DiscountPercent = discount > 0 ? (discount / subtotal) * 100 : 0,
            LineItems = lineItems
        };
    }

    public async Task<BookingConfirmationResult> ConfirmBookingAsync(ConfirmBookingInput input, CancellationToken cancellationToken = default)
    {
        // Validate soft reserve
        var softReserve = await _context.SoftReserves
            .FirstOrDefaultAsync(s => s.Id == input.SoftReserveId && s.SessionId == input.SessionId, cancellationToken);

        if (softReserve == null)
            return new BookingConfirmationResult { Success = false, IsNotFound = true, Error = "Your time slot reservation was not found. Please go back and select a new time." };

        if (softReserve.Status != SoftReserveStatus.Active)
            return new BookingConfirmationResult { Success = false, IsAlreadyProcessed = true, Error = "This reservation has already been processed. Please start a new booking." };

        if (softReserve.ExpiresAt <= DateTime.UtcNow)
        {
            softReserve.Status = SoftReserveStatus.Expired;
            await _context.SaveChangesAsync(cancellationToken); // persist expiry before returning early — outside main transaction
            return new BookingConfirmationResult { Success = false, IsExpired = true, Error = "Your time slot has expired. Please go back and select a new time." };
        }

        // Begin a single atomic transaction for all booking-related writes.
        // If any step fails, the entire booking is rolled back to prevent partial data.
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {

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
            IsFirstTime = input.IsFirstTime,
        }, cancellationToken);

        if (!pricing.Success)
            return new BookingConfirmationResult { Success = false, Error = pricing.Error };

        // Log price mismatch but always use server-calculated price
        var tolerance = pricing.Total * 0.10m;
        if (Math.Abs(input.Total - pricing.Total) > tolerance)
        {
            _logger.LogWarning("Price mismatch detected - Expected: {Expected}, Received: {Received}", pricing.Total, input.Total);
        }

        // Create or find user
        var (customerId, authToken, isNewUser, isGuest) = await ResolveCustomerAsync(input, cancellationToken);

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
            RecurrenceType = RecurrenceType.None,
            Source = OrderSource.Website,
            ContactName = input.ContactName,
            ContactPhone = input.ContactPhone,
            ContactEmail = input.ContactEmail,
            SpecialInstructions = input.SpecialInstructions,
            PreferredStartTime = softReserve.ScheduledStart.TimeOfDay,
            EstimatedDurationMinutes = (int)(softReserve.ScheduledEnd - softReserve.ScheduledStart).TotalMinutes
        };

        // Batch-load rooms and additional services before adding order (no Id yet)
        var roomTypes = new Dictionary<int, CleaningPlaceRoom>();
        if (input.Rooms?.Any() == true)
        {
            var roomIds = input.Rooms.Select(r => r.RoomId).ToList();
            var loaded = await _context.CleaningPlaceRooms
                .AsNoTracking()
                .Where(r => roomIds.Contains(r.Id))
                .ToListAsync(cancellationToken);
            foreach (var r in loaded) roomTypes[r.Id] = r;
        }

        var additionalTypes = new Dictionary<int, AdditionalServiceType>();
        if (input.AdditionalServiceIds?.Any() == true)
        {
            var ids = input.AdditionalServiceIds;
            var additionals = await _context.AdditionalServiceTypes
                .Where(a => ids.Contains(a.Id))
                .ToListAsync(cancellationToken);
            foreach (var a in additionals) additionalTypes[a.Id] = a;
        }

        _context.ServiceOrders.Add(order);
        try
        {
            await _context.SaveChangesAsync(cancellationToken); // needed to get order.Id for FK relationships
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
        {
            // Two concurrent requests raced through ResolveCustomerAsync with the same email.
            // The other request won the insert — re-fetch that user and update the order's CustomerId.
            _logger.LogWarning(
                "Duplicate user insert race condition for email {Email} — re-fetching existing user",
                input.ContactEmail);

            var racedUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == input.ContactEmail, cancellationToken);

            if (racedUser == null)
                throw; // A different constraint was violated — let it propagate

            // Detach the conflicting new user from the context to avoid further tracking issues
            var conflictEntry = _context.ChangeTracker.Entries<ApplicationUser>()
                .FirstOrDefault(e => e.Entity.Email == input.ContactEmail && e.State == Microsoft.EntityFrameworkCore.EntityState.Added);
            if (conflictEntry != null)
                conflictEntry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;

            order.CustomerId = racedUser.Id;
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Save room selections
        foreach (var room in input.Rooms ?? [])
        {
            if (roomTypes.TryGetValue(room.RoomId, out var roomType))
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

        // Save additional service items
        foreach (var additionalId in input.AdditionalServiceIds ?? [])
        {
            if (additionalTypes.TryGetValue(additionalId, out var additional))
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
        await _context.SaveChangesAsync(cancellationToken); // needed to get meet.Id for slot conversion

        // Convert soft reserve to meeting
        softReserve.Status = SoftReserveStatus.Converted;
        softReserve.ServiceOrderId = order.Id;
        softReserve.CustomerId = customerId;

        await ConvertSoftReserveToMeetingOccupancyAsync(softReserve.Id, meet.Id, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order confirmed - OrderId: {OrderId}, MeetId: {MeetId}, Total: {Total}",
            order.Id, meet.Id, order.Total);

        await transaction.CommitAsync(cancellationToken);

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

        } // end try
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "ConfirmBookingAsync failed and was rolled back - SoftReserve {SoftReserveId}", input.SoftReserveId);
            throw;
        }
    }

    /// <summary>
    /// Resolves or creates the customer for a booking.
    /// Returns (customerId, authToken, isNewUser, isGuest).
    /// </summary>
    private async Task<(string? CustomerId, AuthTokenResult? AuthToken, bool IsNewUser, bool IsGuest)>
        ResolveCustomerAsync(ConfirmBookingInput input, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(input.CustomerId))
            return (input.CustomerId, null, false, false);

        // Check if email already exists
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == input.ContactEmail, cancellationToken);

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
            FirstName = (input.ContactName ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(),
            LastName = (input.ContactName ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries) is { Length: > 1 } nameParts
                ? string.Join(' ', nameParts.Skip(1))
                : null
        };

        newUser.PasswordHash = _passwordHasher.HashPassword(newUser, input.Password);
        _context.Users.Add(newUser);

        var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == Roles.Customer, cancellationToken);
        if (customerRole != null)
        {
            _context.UserRoles.Add(new IdentityUserRole<string>
            {
                UserId = newUser.Id,
                RoleId = customerRole.Id
            });
        }

        var accessToken = _jwtService.GenerateAccessToken(newUser.Id, newUser.Email!, new[] { Roles.Customer });
        var refreshTokenValue = _jwtService.GenerateRefreshToken();

        var jti = new JwtSecurityTokenHandler().ReadJwtToken(accessToken).Id ?? Guid.NewGuid().ToString();

        _context.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshTokenValue,
            JwtId = jti,
            UserId = newUser.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        });

        _logger.LogInformation("User created automatically during booking: {Email}", input.ContactEmail);

        return (newUser.Id, new AuthTokenResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsNewUser = true
        }, true, false);
    }

    /// <summary>
    /// Returns true when a DbUpdateException wraps a duplicate key / unique constraint violation.
    /// Covers MySQL error 1062 (Pomelo) and generic UNIQUE constraint messages.
    /// </summary>
    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        var inner = ex.InnerException?.Message ?? ex.Message;
        return inner.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase) ||
               inner.Contains("UNIQUE constraint", StringComparison.OrdinalIgnoreCase) ||
               inner.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
               inner.Contains("1062");
    }

    /// <summary>
    /// Converts SlotOccupancy entries from SoftReserve to Meeting type.
    /// </summary>
    private async Task ConvertSoftReserveToMeetingOccupancyAsync(int softReserveId, int meetId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var slots = await _context.SlotOccupancies
            .Where(s => s.OccupancyType == OccupancyType.SoftReserve && s.ReferenceId == softReserveId)
            .ToListAsync(cancellationToken);

        foreach (var slot in slots)
        {
            slot.OccupancyType = OccupancyType.Meeting;
            slot.ReferenceId = meetId;
            slot.ExpiresAt = null;
            slot.UpdatedAt = now;
        }
    }
}
