using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Api.Services;
using SavedByTheMaid.Infrastructure.Data;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Api.Controllers;

/// <summary>
/// Public API for the booking wizard
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class BookingController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BookingController> _logger;
    private readonly IEmailService _emailService;
    private readonly ISchedulingService _schedulingService;
    private readonly IBookingService _bookingService;

    public BookingController(
        ApplicationDbContext context,
        ILogger<BookingController> logger,
        IEmailService emailService,
        ISchedulingService schedulingService,
        IBookingService bookingService)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
        _schedulingService = schedulingService;
        _bookingService = bookingService;
    }

    #region Step 1 - Address and Coverage

    /// <summary>
    /// Checks if a ZIP code has coverage and returns the service area
    /// </summary>
    [HttpGet("coverage/{zipCode}")]
    public async Task<ActionResult<CoverageResponse>> CheckCoverage(string zipCode)
    {
        var serviceAreaZip = await _context.ServiceAreaZips
            .AsNoTracking()
            .Include(z => z.ServiceArea)
            .FirstOrDefaultAsync(z => z.ZipCode == zipCode && !z.IsDeleted);

        if (serviceAreaZip?.ServiceArea == null || !serviceAreaZip.ServiceArea.IsActive)
        {
            return Ok(new CoverageResponse
            {
                IsCovered = false,
                Message = "Sorry, we don't cover this area yet."
            });
        }

        return Ok(new CoverageResponse
        {
            IsCovered = true,
            ServiceAreaId = serviceAreaZip.ServiceAreaId,
            ServiceAreaName = serviceAreaZip.ServiceArea.Name,
            City = serviceAreaZip.City,
            State = serviceAreaZip.State,
            County = serviceAreaZip.County,
            Message = $"Great! We service {serviceAreaZip.City ?? "your area"}, {serviceAreaZip.State ?? ""}."
        });
    }

    #endregion

    #region Step 2 - Service Catalog

    /// <summary>
    /// Gets available cleaning place types
    /// </summary>
    [HttpGet("cleaning-places")]
    public async Task<ActionResult<IEnumerable<CleaningPlaceDto>>> GetCleaningPlaces()
    {
        var places = await _context.CleaningPlaces
            .AsNoTracking()
            .Where(p => p.IsActive && !p.IsDeleted)
            .Include(p => p.Rooms.Where(r => r.IsActive && !r.IsDeleted))
            .Select(p => new CleaningPlaceDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Rooms = p.Rooms.OrderBy(r => r.DisplayOrder).Select(r => new CleaningPlaceRoomDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    BaseMinutes = r.BaseMinutes,
                    BasePrice = r.BasePrice
                }).ToList()
            })
            .ToListAsync();

        return Ok(places);
    }

    /// <summary>
    /// Gets available service types
    /// </summary>
    [HttpGet("service-types")]
    public async Task<ActionResult<IEnumerable<ServiceTypeDto>>> GetServiceTypes()
    {
        var types = await _context.ServiceTypes
            .AsNoTracking()
            .Where(t => t.IsActive && !t.IsDeleted)
            .OrderBy(t => t.DisplayOrder)
            .Select(t => new ServiceTypeDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Price = t.Price,
                PricePerBedroom = t.PricePerBedroom,
                PricePerBathroom = t.PricePerBathroom,
                EstimatedMinutes = t.EstimatedMinutes,
                MinutesPerBedroom = t.MinutesPerBedroom,
                MinutesPerBathroom = t.MinutesPerBathroom
            })
            .ToListAsync();

        return Ok(types);
    }

    /// <summary>
    /// Gets available additional services
    /// </summary>
    [HttpGet("additional-services")]
    public async Task<ActionResult<IEnumerable<AdditionalServiceDto>>> GetAdditionalServices()
    {
        var services = await _context.AdditionalServiceTypes
            .AsNoTracking()
            .Where(s => s.IsActive && !s.IsDeleted)
            .Select(s => new AdditionalServiceDto
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                Price = s.Price,
                AdditionalMinutes = s.AdditionalMinutes
            })
            .ToListAsync();

        return Ok(services);
    }

    /// <summary>
    /// Gets recurrence discounts
    /// </summary>
    [HttpGet("recurrence-discounts")]
    public async Task<ActionResult<IEnumerable<RecurrenceDiscountDto>>> GetRecurrenceDiscounts()
    {
        var discounts = await _context.RecurrenceDiscounts
            .AsNoTracking()
            .Where(d => d.IsActive && !d.IsDeleted)
            .Select(d => new RecurrenceDiscountDto
            {
                RecurrenceType = d.RecurrenceType,
                RecurrenceTypeName = d.RecurrenceType.ToString(),
                DiscountPercent = d.DiscountPercent
            })
            .ToListAsync();

        return Ok(discounts);
    }

    #endregion

    #region Step 3 - Price and Time Estimate

    /// <summary>
    /// Calculates estimated price and time based on customer selection
    /// </summary>
    [HttpPost("estimate")]
    public async Task<ActionResult<EstimateResponse>> CalculateEstimate(EstimateRequest request)
    {
        if (request.ServiceTypeId <= 0)
            return BadRequest("ServiceTypeId must be greater than 0");

        if (request.Rooms?.Any(r => r.Quantity < 0) == true)
            return BadRequest("Room quantity cannot be negative");

        if (request.SquareFootage.HasValue && (request.SquareFootage < 100 || request.SquareFootage > 50000))
            return BadRequest("SquareFootage must be between 100 and 50,000 sq ft");

        var pricing = await _bookingService.CalculatePricingAsync(new PricingInput
        {
            ServiceTypeId = request.ServiceTypeId,
            Rooms = request.Rooms?.Select(r => new RoomPricingItem(r.RoomId, r.Quantity)).ToList(),
            Bedrooms = request.Bedrooms,
            Bathrooms = request.Bathrooms,
            AdditionalServiceIds = request.AdditionalServiceIds,
            SquareFootage = request.SquareFootage,
            DirtLevel = request.DirtLevel,
            HasPets = request.HasPets,
            HasElevator = request.HasElevator,
            IsFirstTime = request.IsFirstTime,
            RecurrenceType = request.RecurrenceType
        });

        if (!pricing.Success)
            return BadRequest(new { message = pricing.Error });

        return Ok(new EstimateResponse
        {
            EstimatedMinutes = pricing.EstimatedMinutes,
            FormattedDuration = FormatDuration(pricing.EstimatedMinutes),
            Subtotal = pricing.Subtotal,
            Discount = pricing.Discount,
            Total = pricing.Total,
            RecurrenceType = request.RecurrenceType,
            DiscountPercent = pricing.DiscountPercent
        });
    }

    #endregion

    #region Step 4 - Availability

    /// <summary>
    /// Gets available time slots for a specific date
    /// </summary>
    [HttpPost("availability")]
    public async Task<ActionResult<AvailabilityResponse>> GetAvailability(AvailabilityRequest request)
    {
        // Validate service area
        var serviceAreaZip = await _context.ServiceAreaZips
            .FirstOrDefaultAsync(z => z.ZipCode == request.ZipCode);

        if (serviceAreaZip == null)
            return BadRequest("ZIP code not covered");

        var serviceAreaId = serviceAreaZip.ServiceAreaId;
        var date = request.Date.Date;
        var dayOfWeek = date.DayOfWeek;

        // Get employees covering this area
        var employeesInZone = await _context.EmployeeServiceAreas
            .Where(e => e.ServiceAreaId == serviceAreaId && !e.IsDeleted)
            .Select(e => e.EmployeeId)
            .ToListAsync();

        // Get active employees with schedule for that day
        var employees = await _context.Employees
            .Where(e => e.IsActive && !e.IsDeleted)
            .Where(e => employeesInZone.Contains(e.Id))
            .Include(e => e.Schedules.Where(s => s.DayOfWeek == dayOfWeek && s.IsAvailable))
            .Include(e => e.TimeOffs.Where(t => 
                t.Status == TimeOffStatus.Approved &&
                t.StartDateTime <= date.AddDays(1) &&
                t.EndDateTime >= date))
            .ToListAsync();

        // Filter by required equipment if specified
        if (request.RequiredEquipmentIds?.Any() == true)
        {
            var employeesWithEquipment = await _context.EmployeeEquipment
                .Where(e => request.RequiredEquipmentIds.Contains(e.EquipmentId) && e.IsAvailable)
                .Select(e => e.EmployeeId)
                .Distinct()
                .ToListAsync();

            employees = employees.Where(e => employeesWithEquipment.Contains(e.Id)).ToList();
        }

        var slots = new List<TimeSlotDto>();

        foreach (var employee in employees)
        {
            var schedule = employee.Schedules.FirstOrDefault();
            if (schedule == null) continue;

            // Check if employee has time-off that day
            var hasTimeOff = employee.TimeOffs.Any(t =>
                (t.IsAllDay) ||
                (t.StartDateTime.Date <= date && t.EndDateTime.Date >= date));

            if (hasTimeOff) continue;

            // Get existing meetings for that day
            var existingMeetings = await _context.ServiceMeets
                .Where(m => m.AssignedEmployeeId == employee.Id)
                .Where(m => m.ScheduledStart.Date == date)
                .Where(m => m.Status != MeetStatus.Cancelled && m.Status != MeetStatus.NoShow)
                .Select(m => new { m.ScheduledStart, m.ScheduledEnd })
                .ToListAsync();

            // Get active soft reserves
            var activeSoftReserves = await _context.SoftReserves
                .Where(s => s.EmployeeId == employee.Id)
                .Where(s => s.ScheduledStart.Date == date)
                .Where(s => s.Status == SoftReserveStatus.Active && s.ExpiresAt > DateTime.UtcNow)
                .Select(s => new { s.ScheduledStart, s.ScheduledEnd })
                .ToListAsync();

            // Combine occupancy
            var occupied = existingMeetings
                .Concat(activeSoftReserves)
                .Select(o => (Start: o.ScheduledStart, End: o.ScheduledEnd))
                .ToList();

            // Generate slots every 30 minutes (accounting for buffer between services)
            var bufferMinutes = schedule.BufferMinutes;
            var slotStart = date.Add(schedule.StartTime);
            var dayEnd = date.Add(schedule.EndTime);
            var duration = TimeSpan.FromMinutes(request.EstimatedMinutes);

            while (slotStart.Add(duration) <= dayEnd)
            {
                var slotEnd = slotStart.Add(duration);

                // Check for overlap (with buffer)
                var hasConflict = occupied.Any(o =>
                    slotStart < o.End.AddMinutes(bufferMinutes) && slotEnd.AddMinutes(bufferMinutes) > o.Start);

                if (!hasConflict)
                {
                    // Check if an identical slot already exists
                    var existingSlot = slots.FirstOrDefault(s => 
                        s.StartTime == slotStart.TimeOfDay);

                    if (existingSlot != null)
                    {
                        // Add employee to existing slot
                        existingSlot.AvailableEmployeeIds.Add(employee.Id);
                    }
                    else
                    {
                        slots.Add(new TimeSlotDto
                        {
                            Date = date,
                            StartTime = slotStart.TimeOfDay,
                            EndTime = slotEnd.TimeOfDay,
                            FormattedTime = $"{slotStart:hh:mm tt} - {slotEnd:hh:mm tt}",
                            AvailableEmployeeIds = new List<int> { employee.Id }
                        });
                    }
                }

                slotStart = slotStart.AddMinutes(30);
            }
        }

        return Ok(new AvailabilityResponse
        {
            Date = date,
            ZipCode = request.ZipCode,
            ServiceAreaId = serviceAreaId,
            Slots = slots.OrderBy(s => s.StartTime).ToList(),
            TotalSlotsAvailable = slots.Count
        });
    }

    #endregion

    #region Step 5 - Soft Reserve (anti-collision)

    /// <summary>
    /// Creates a temporary reserve (soft reserve) while the customer completes checkout.
    /// Uses SlotOccupancy with UNIQUE constraint for DB-level anti-collision.
    /// </summary>
    [HttpPost("soft-reserve")]
    public async Task<ActionResult<SoftReserveResponse>> CreateSoftReserve(CreateSoftReserveRequest request)
    {
        // Get ServiceAreaId from ZipCode
        var serviceAreaZip = await _context.ServiceAreaZips
            .Include(z => z.ServiceArea)
            .FirstOrDefaultAsync(z => z.ZipCode == request.ZipCode && z.ServiceArea.IsActive);

        if (serviceAreaZip == null)
        {
            return BadRequest(new { message = "ZIP code not covered." });
        }

        var serviceAreaId = serviceAreaZip.ServiceAreaId;
        var sessionId = request.SessionId ?? Guid.NewGuid().ToString();
        var ttlMinutes = 15;
        var expiresAt = DateTime.UtcNow.AddMinutes(ttlMinutes);

        var startDateTime = request.Date.Date.Add(request.StartTime);
        var endDateTime = startDateTime.AddMinutes(request.EstimatedMinutes);

        // 1. Verify employee has a work schedule for that day
        var dayOfWeek = request.Date.DayOfWeek;
        var schedule = await _context.EmployeeSchedules
            .FirstOrDefaultAsync(s => 
                s.EmployeeId == request.EmployeeId &&
                s.DayOfWeek == dayOfWeek &&
                s.IsAvailable);

        if (schedule == null)
        {
            return BadRequest(new { message = "Employee does not work on that day." });
        }

        // Verify the time is within the work shift
        if (request.StartTime < schedule.StartTime || request.StartTime >= schedule.EndTime)
        {
            return BadRequest(new { message = "Time is outside the employee's work shift." });
        }

        // 2. Check conflicts using centralized service (TimeOffs and other Bookings)
        var conflict = await _schedulingService.CheckConflictsAsync(request.EmployeeId, startDateTime, endDateTime);
        if (conflict != null)
        {
            _logger.LogWarning("Conflict detected creating SoftReserve: {Message}", conflict.Message);
            return Conflict(new { message = conflict.Message, details = conflict.Details });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Create soft reserve first to get the ID
            var softReserve = new SoftReserve
            {
                SessionId = sessionId,
                CustomerId = request.CustomerId,
                EmployeeId = request.EmployeeId,
                ServiceAreaId = serviceAreaId,
                ScheduledStart = startDateTime,
                ScheduledEnd = endDateTime,
                ExpiresAt = expiresAt,
                Status = SoftReserveStatus.Active
            };

            _context.SoftReserves.Add(softReserve);
            await _context.SaveChangesAsync();

            // Insert SlotOccupancy rows using the service
            // UNIQUE constraint on (EmployeeId, SlotStart) prevents double-booking
            await _schedulingService.AcquireSlotsAsync(
                request.EmployeeId, 
                startDateTime, 
                endDateTime, 
                OccupancyType.SoftReserve, 
                softReserve.Id, 
                expiresAt);

            // Commit transaction
            await transaction.CommitAsync();

            _logger.LogInformation(
                "SoftReserve {SoftReserveId} created for employee {EmployeeId}",
                softReserve.Id, request.EmployeeId);

            return Ok(new SoftReserveResponse
            {
                SoftReserveId = softReserve.Id,
                SessionId = sessionId,
                ScheduledStart = startDateTime,
                ScheduledEnd = endDateTime,
                ExpiresAt = softReserve.ExpiresAt,
                TtlSeconds = ttlMinutes * 60,
                Message = $"Time slot reserved for {ttlMinutes} minutes. Complete payment to confirm."
            });
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Constraint violation = slot already taken by another user
            _logger.LogWarning(
                "Slot conflict for employee {EmployeeId} at {Start} - {End}",
                request.EmployeeId, startDateTime, endDateTime);
            
            return Conflict(new { message = "This time slot is no longer available. Someone else has reserved it." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating soft reserve");
            throw;
        }
    }

    /// <summary>
    /// Cancels a soft reserve manually
    /// </summary>
    [HttpDelete("soft-reserve/{id}")]
    public async Task<IActionResult> CancelSoftReserve(int id, [FromQuery] string sessionId)
    {
        var softReserve = await _context.SoftReserves
            .FirstOrDefaultAsync(s => s.Id == id && s.SessionId == sessionId);

        if (softReserve == null)
            return NotFound();

        if (softReserve.Status != SoftReserveStatus.Active)
            return BadRequest(new { message = "This reservation has already been processed or expired. Please select a new time." });

        softReserve.Status = SoftReserveStatus.Cancelled;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Extends the time of a soft reserve
    /// </summary>
    [HttpPost("soft-reserve/{id}/extend")]
    public async Task<ActionResult<SoftReserveResponse>> ExtendSoftReserve(int id, [FromQuery] string sessionId)
    {
        var softReserve = await _context.SoftReserves
            .FirstOrDefaultAsync(s => s.Id == id && s.SessionId == sessionId);

        if (softReserve == null)
            return NotFound();

        if (softReserve.Status != SoftReserveStatus.Active || softReserve.ExpiresAt <= DateTime.UtcNow)
            return BadRequest(new { message = "Your time slot has expired. Please go back and select a new time." });

        // Limit extensions to prevent indefinite slot blocking
        const int maxExtensions = 2;
        if (softReserve.ExtensionCount >= maxExtensions)
            return BadRequest(new { message = $"Maximum of {maxExtensions} extensions reached. Please complete your booking or select a new time." });

        // Extend 10 more minutes from current expiration (not from now)
        var newExpiry = softReserve.ExpiresAt > DateTime.UtcNow
            ? softReserve.ExpiresAt.AddMinutes(10)
            : DateTime.UtcNow.AddMinutes(10);
        softReserve.ExpiresAt = newExpiry;
        softReserve.ExtensionCount++;

        // Also extend the SlotOccupancy expiry to match
        var slotsToExtend = await _context.SlotOccupancies
            .Where(s => s.OccupancyType == OccupancyType.SoftReserve && s.ReferenceId == softReserve.Id)
            .ToListAsync();
        foreach (var slot in slotsToExtend)
        {
            slot.ExpiresAt = newExpiry;
        }

        await _context.SaveChangesAsync();

        return Ok(new SoftReserveResponse
        {
            SoftReserveId = softReserve.Id,
            SessionId = softReserve.SessionId,
            ScheduledStart = softReserve.ScheduledStart,
            ScheduledEnd = softReserve.ScheduledEnd,
            ExpiresAt = softReserve.ExpiresAt,
            TtlSeconds = (int)(softReserve.ExpiresAt - DateTime.UtcNow).TotalSeconds,
            Message = "Reservation extended."
        });
    }

    #endregion

    #region Step 6 - Confirm Booking

    /// <summary>
    /// Confirms the booking: validates reserve, creates order + meeting, converts slots.
    /// </summary>
    [HttpPost("confirm")]
    public async Task<ActionResult<BookingConfirmationResponse>> ConfirmBooking(ConfirmBookingRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var result = await _bookingService.ConfirmBookingAsync(new ConfirmBookingInput
            {
                SoftReserveId = request.SoftReserveId,
                SessionId = request.SessionId,
                CustomerId = request.CustomerId,
                ZipCode = request.ZipCode,
                Address = request.Address,
                AddressLine2 = request.AddressLine2,
                City = request.City,
                State = request.State,
                ServiceTypeId = request.ServiceTypeId,
                CleaningPlaceId = request.CleaningPlaceId,
                Bedrooms = request.Bedrooms,
                Bathrooms = request.Bathrooms,
                SquareFootage = request.SquareFootage,
                DirtLevel = request.DirtLevel,
                HasPets = request.HasPets,
                FloorLevel = request.FloorLevel,
                HasElevator = request.HasElevator,
                AdditionalServiceIds = request.AdditionalServiceIds,
                Rooms = request.Rooms?.Select(r => new RoomPricingItem(r.RoomId, r.Quantity)).ToList(),
                Total = request.Total,
                RecurrenceType = request.RecurrenceType,
                RecurrenceEndDate = request.RecurrenceEndDate,
                ContactName = request.ContactName,
                ContactPhone = request.ContactPhone,
                ContactEmail = request.ContactEmail,
                Password = request.Password,
                SpecialInstructions = request.SpecialInstructions
            });

            if (!result.Success)
            {
                if (result.IsNotFound) return NotFound(new { message = result.Error });
                if (result.IsExpired || result.IsAlreadyProcessed) return BadRequest(new { message = result.Error });
                return BadRequest(new { message = result.Error });
            }

            await transaction.CommitAsync();

            // Send confirmation email (fire-and-forget)
            if (!string.IsNullOrEmpty(request.ContactEmail))
            {
                var serviceType = await _context.ServiceTypes.FindAsync(request.ServiceTypeId);
                var softReserve = await _context.SoftReserves
                    .FirstOrDefaultAsync(s => s.Id == request.SoftReserveId);
                var employee = softReserve != null
                    ? await _context.Employees.FindAsync(softReserve.EmployeeId)
                    : null;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendBookingConfirmationAsync(
                            request.ContactEmail,
                            new BookingConfirmationEmail(
                                CustomerName: request.ContactName ?? "Valued Customer",
                                ServiceType: serviceType?.Name ?? "Cleaning Service",
                                ScheduledDate: result.ScheduledStart.Date,
                                ScheduledTime: result.ScheduledStart.ToString("h:mm tt"),
                                Address: $"{request.Address}, {request.City}, {request.State} {request.ZipCode}",
                                TotalAmount: result.Total,
                                EmployeeName: employee != null ? $"{employee.FirstName} {employee.LastName}" : "Our professional cleaner",
                                EstimatedDuration: (int)(result.ScheduledEnd - result.ScheduledStart).TotalMinutes
                            ));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send confirmation email for Order {OrderId}", result.OrderId);
                    }
                });
            }

            return Ok(new BookingConfirmationResponse
            {
                OrderId = result.OrderId,
                MeetId = result.MeetId,
                ConfirmationNumber = result.ConfirmationNumber,
                ScheduledStart = result.ScheduledStart,
                ScheduledEnd = result.ScheduledEnd,
                Total = result.Total,
                OrderStatus = result.OrderStatus,
                Message = result.Message,
                AuthToken = result.AuthToken != null ? new AuthToken
                {
                    AccessToken = result.AuthToken.AccessToken,
                    RefreshToken = result.AuthToken.RefreshToken,
                    ExpiresAt = result.AuthToken.ExpiresAt,
                    IsNewUser = result.AuthToken.IsNewUser
                } : null,
                IsGuest = result.IsGuest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming booking");
            throw;
        }
    }

    #endregion

    #region Helpers


    /// <summary>
    /// Checks if an exception is a UNIQUE constraint violation
    /// </summary>
    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // MySQL: error 1062 (Duplicate entry)
        // Pomelo MySQL: checks InnerException
        var inner = ex.InnerException?.Message ?? ex.Message;
        return inner.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase) ||
               inner.Contains("UNIQUE constraint", StringComparison.OrdinalIgnoreCase) ||
               inner.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
               inner.Contains("1062");
    }

    private static string FormatDuration(int minutes)
    {
        var hours = minutes / 60;
        var mins = minutes % 60;
        return hours > 0 
            ? $"{hours}h {mins}min" 
            : $"{mins} min";
    }

    #endregion
}

#region DTOs

public record CoverageResponse
{
    public bool IsCovered { get; init; }
    public int? ServiceAreaId { get; init; }
    public string? ServiceAreaName { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? County { get; init; }
    public string Message { get; init; } = "";
}

public record CleaningPlaceDto
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public List<CleaningPlaceRoomDto> Rooms { get; init; } = new();
}

public record CleaningPlaceRoomDto
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public int BaseMinutes { get; init; }
    public decimal BasePrice { get; init; }
}

public record ServiceTypeDto
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public decimal PricePerBedroom { get; init; }
    public decimal PricePerBathroom { get; init; }
    public int EstimatedMinutes { get; init; }
    public int MinutesPerBedroom { get; init; }
    public int MinutesPerBathroom { get; init; }
}

public record AdditionalServiceDto
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public int AdditionalMinutes { get; init; }
}

public record RecurrenceDiscountDto
{
    public RecurrenceType RecurrenceType { get; init; }
    public string RecurrenceTypeName { get; init; } = "";
    public decimal DiscountPercent { get; init; }
}

public record EstimateRequest
{
    [Required(ErrorMessage = "Service type is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid service type ID")]
    public int ServiceTypeId { get; init; }

    public int? CleaningPlaceId { get; init; }
    public List<RoomSelection> Rooms { get; init; } = new();
    public List<int> AdditionalServiceIds { get; init; } = new();

    [Range(1, 20, ErrorMessage = "Bedrooms must be between 1 and 20")]
    public int Bedrooms { get; init; } = 1;

    [Range(1, 20, ErrorMessage = "Bathrooms must be between 1 and 20")]
    public int Bathrooms { get; init; } = 1;

    [Range(0, 50000, ErrorMessage = "Square footage must be between 0 and 50,000")]
    public int? SquareFootage { get; init; }

    public DirtLevel DirtLevel { get; init; } = DirtLevel.Normal;
    public bool HasPets { get; init; }
    public bool HasElevator { get; init; } = true;
    public bool IsFirstTime { get; init; } = true;
    public RecurrenceType RecurrenceType { get; init; } = RecurrenceType.None;
}

public record RoomSelection
{
    public int RoomId { get; init; }
    public int Quantity { get; init; } = 1;
}

public record EstimateResponse
{
    public int EstimatedMinutes { get; init; }
    public string FormattedDuration { get; init; } = "";
    public decimal Subtotal { get; init; }
    public decimal Discount { get; init; }
    public decimal Total { get; init; }
    public RecurrenceType RecurrenceType { get; init; }
    public decimal DiscountPercent { get; init; }
}

public record AvailabilityRequest
{
    [Required(ErrorMessage = "ZIP code is required")]
    [StringLength(10, MinimumLength = 5, ErrorMessage = "ZIP code must be between 5 and 10 characters")]
    [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Invalid ZIP code format")]
    public string ZipCode { get; init; } = "";
    
    [Required(ErrorMessage = "Date is required")]
    public DateTime Date { get; init; }
    
    [Required(ErrorMessage = "Estimated duration is required")]
    [Range(30, 480, ErrorMessage = "Duration must be between 30 and 480 minutes")]
    public int EstimatedMinutes { get; init; }
    
    public List<int>? RequiredEquipmentIds { get; init; }
}

public record AvailabilityResponse
{
    public DateTime Date { get; init; }
    public string ZipCode { get; init; } = "";
    public int ServiceAreaId { get; init; }
    public List<TimeSlotDto> Slots { get; init; } = new();
    public int TotalSlotsAvailable { get; init; }
}

public record TimeSlotDto
{
    public DateTime Date { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public string FormattedTime { get; init; } = "";
    public List<int> AvailableEmployeeIds { get; init; } = new();
}

public record CreateSoftReserveRequest
{
    [StringLength(100)]
    public string? SessionId { get; init; }
    
    [StringLength(100)]
    public string? CustomerId { get; init; }
    
    [Required(ErrorMessage = "Employee is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid employee ID")]
    public int EmployeeId { get; init; }
    
    [Required(ErrorMessage = "ZIP code is required")]
    [StringLength(10, MinimumLength = 5)]
    public string ZipCode { get; init; } = "";
    
    [Required(ErrorMessage = "Date is required")]
    public DateTime Date { get; init; }
    
    [Required(ErrorMessage = "Start time is required")]
    public TimeSpan StartTime { get; init; }
    
    [Required(ErrorMessage = "Estimated duration is required")]
    [Range(30, 480, ErrorMessage = "Duration must be between 30 and 480 minutes")]
    public int EstimatedMinutes { get; init; }
}

public record SoftReserveResponse
{
    public int SoftReserveId { get; init; }
    public string SessionId { get; init; } = "";
    public DateTime ScheduledStart { get; init; }
    public DateTime ScheduledEnd { get; init; }
    public DateTime ExpiresAt { get; init; }
    public int TtlSeconds { get; init; }
    public string Message { get; init; } = "";
}

public record ConfirmBookingRequest
{
    [Required(ErrorMessage = "Reservation is required")]
    [Range(1, int.MaxValue)]
    public int SoftReserveId { get; init; }
    
    [Required(ErrorMessage = "Session ID is required")]
    [StringLength(100, MinimumLength = 10)]
    public string SessionId { get; init; } = "";
    
    [StringLength(100)]
    public string? CustomerId { get; init; }
    
    public bool PaymentConfirmed { get; init; }
    
    // Address
    [Required(ErrorMessage = "ZIP code is required")]
    [StringLength(10, MinimumLength = 5)]
    public string ZipCode { get; init; } = "";
    
    [Required(ErrorMessage = "Address is required")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Address must be between 5 and 500 characters")]
    public string Address { get; init; } = "";
    
    [StringLength(200)]
    public string? AddressLine2 { get; init; }
    
    [StringLength(100)]
    public string? City { get; init; }
    
    [StringLength(50)]
    public string? State { get; init; }
    
    // Service
    [Required(ErrorMessage = "Service type is required")]
    [Range(1, int.MaxValue)]
    public int ServiceTypeId { get; init; }
    
    public int? CleaningPlaceId { get; init; }
    
    [Range(0, 20, ErrorMessage = "Invalid number of bedrooms")]
    public int Bedrooms { get; init; } = 1;
    
    [Range(0, 20, ErrorMessage = "Invalid number of bathrooms")]
    public int Bathrooms { get; init; } = 1;
    
    [Range(0, 50000)]
    public int? SquareFootage { get; init; }
    
    public DirtLevel DirtLevel { get; init; } = DirtLevel.Normal;
    public bool HasPets { get; init; }
    
    [Range(0, 100)]
    public int? FloorLevel { get; init; }
    
    public bool HasElevator { get; init; } = true;
    public List<int>? AdditionalServiceIds { get; init; }
    public List<RoomSelection>? Rooms { get; init; }

    // Amounts
    [Range(0, 100000)]
    public decimal Subtotal { get; init; }
    
    [Range(0, 100000)]
    public decimal Tax { get; init; }
    
    [Range(0, 100000)]
    public decimal Discount { get; init; }
    
    [Range(0, 100000)]
    public decimal Total { get; init; }
    
    // Recurrence
    public RecurrenceType RecurrenceType { get; init; } = RecurrenceType.None;
    public DateTime? RecurrenceEndDate { get; init; }
    
    // Contact
    [StringLength(100)]
    public string? ContactName { get; init; }
    
    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(20)]
    public string? ContactPhone { get; init; }
    
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(256)]
    public string ContactEmail { get; init; } = "";
    
    // Password to create account if user doesn't exist
    [StringLength(100, MinimumLength = 8)]
    public string? Password { get; init; }
    
    [StringLength(1000)]
    public string? SpecialInstructions { get; init; }
}

public record BookingConfirmationResponse
{
    public int OrderId { get; init; }
    public int MeetId { get; init; }
    public string ConfirmationNumber { get; init; } = "";
    public DateTime ScheduledStart { get; init; }
    public DateTime ScheduledEnd { get; init; }
    public decimal Total { get; init; }
    public string OrderStatus { get; init; } = "";
    public string Message { get; init; } = "";
    public AuthToken? AuthToken { get; init; }
    public bool IsGuest { get; init; }
}

public record AuthToken
{
    public string AccessToken { get; init; } = "";
    public string RefreshToken { get; init; } = "";
    public DateTime ExpiresAt { get; init; }
    public bool IsNewUser { get; init; }
}

#endregion
