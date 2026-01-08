using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Api.Auth;
using SavedByTheMaid.Api.Services;
using SavedByTheMaid.Infrastructure.Data;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Api.Controllers;

/// <summary>
/// API pública para el wizard de reservas
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // Endpoints públicos para clientes
public class BookingController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BookingController> _logger;
    private readonly IEmailService _emailService;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;

    public BookingController(
        ApplicationDbContext context, 
        ILogger<BookingController> logger, 
        IEmailService emailService,
        IJwtService jwtService,
        IPasswordHasher<ApplicationUser> passwordHasher)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
    }

    #region Step 1 - Dirección y Cobertura

    /// <summary>
    /// Verifica si un ZIP code tiene cobertura y devuelve la zona de servicio
    /// </summary>
    [HttpGet("coverage/{zipCode}")]
    public async Task<ActionResult<CoverageResponse>> CheckCoverage(string zipCode)
    {
        var serviceAreaZip = await _context.ServiceAreaZips
            .Include(z => z.ServiceArea)
            .FirstOrDefaultAsync(z => z.ZipCode == zipCode && !z.IsDeleted);

        if (serviceAreaZip?.ServiceArea == null || !serviceAreaZip.ServiceArea.IsActive)
        {
            return Ok(new CoverageResponse
            {
                IsCovered = false,
                Message = "Lo sentimos, aún no damos servicio en esta zona."
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
            Message = $"¡Excelente! Damos servicio en {serviceAreaZip.City ?? "tu zona"}, {serviceAreaZip.State ?? ""}."
        });
    }

    #endregion

    #region Step 2 - Catálogo de Servicios

    /// <summary>
    /// Obtiene los tipos de inmueble disponibles
    /// </summary>
    [HttpGet("cleaning-places")]
    public async Task<ActionResult<IEnumerable<CleaningPlaceDto>>> GetCleaningPlaces()
    {
        var places = await _context.CleaningPlaces
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
    /// Obtiene los tipos de servicio disponibles
    /// </summary>
    [HttpGet("service-types")]
    public async Task<ActionResult<IEnumerable<ServiceTypeDto>>> GetServiceTypes()
    {
        var types = await _context.ServiceTypes
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
    /// Obtiene los servicios adicionales disponibles
    /// </summary>
    [HttpGet("additional-services")]
    public async Task<ActionResult<IEnumerable<AdditionalServiceDto>>> GetAdditionalServices()
    {
        var services = await _context.AdditionalServiceTypes
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
    /// Obtiene los descuentos por recurrencia
    /// </summary>
    [HttpGet("recurrence-discounts")]
    public async Task<ActionResult<IEnumerable<RecurrenceDiscountDto>>> GetRecurrenceDiscounts()
    {
        var discounts = await _context.RecurrenceDiscounts
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

    #region Step 3 - Estimación de Precio y Tiempo

    /// <summary>
    /// Calcula el precio y tiempo estimado basado en la selección del cliente
    /// </summary>
    [HttpPost("estimate")]
    public async Task<ActionResult<EstimateResponse>> CalculateEstimate(EstimateRequest request)
    {
        // Validar entrada
        if (request.ServiceTypeId <= 0)
            return BadRequest("ServiceTypeId debe ser mayor a 0");
        
        if (request.Rooms?.Any(r => r.Quantity < 0) == true)
            return BadRequest("La cantidad de habitaciones no puede ser negativa");
        
        if (request.SquareFootage.HasValue && (request.SquareFootage < 100 || request.SquareFootage > 50000))
            return BadRequest("SquareFootage debe estar entre 100 y 50,000 pies cuadrados");

        // Obtener tipo de servicio
        var serviceType = await _context.ServiceTypes.FindAsync(request.ServiceTypeId);
        if (serviceType == null)
            return BadRequest("Tipo de servicio no válido");

        // Calcular tiempo base
        int totalMinutes = serviceType.EstimatedMinutes;
        decimal totalPrice = serviceType.Price;

        // Agregar tiempo y precio por habitaciones
        foreach (var room in request.Rooms)
        {
            var roomType = await _context.CleaningPlaceRooms.FindAsync(room.RoomId);
            if (roomType != null)
            {
                totalMinutes += roomType.BaseMinutes * room.Quantity;
                totalPrice += roomType.BasePrice * room.Quantity;
            }
        }

        // Agregar servicios adicionales
        foreach (var additionalId in request.AdditionalServiceIds)
        {
            var additional = await _context.AdditionalServiceTypes.FindAsync(additionalId);
            if (additional != null)
            {
                totalMinutes += additional.AdditionalMinutes;
                totalPrice += additional.Price;
            }
        }

        // Aplicar multiplicadores
        var multipliers = await _context.PriceMultipliers
            .Where(m => m.IsActive && !m.IsDeleted)
            .Where(m => m.ServiceTypeId == null || m.ServiceTypeId == request.ServiceTypeId)
            .ToListAsync();

        decimal timeFactor = 1.0m;
        decimal priceFactor = 1.0m;

        foreach (var mult in multipliers)
        {
            bool applies = mult.ConditionType switch
            {
                MultiplierConditionType.SquareFootage when request.SquareFootage.HasValue =>
                    (!mult.MinValue.HasValue || request.SquareFootage >= mult.MinValue) &&
                    (!mult.MaxValue.HasValue || request.SquareFootage <= mult.MaxValue),
                MultiplierConditionType.DirtLevel =>
                    (int)request.DirtLevel == (int)(mult.MinValue ?? 1),
                MultiplierConditionType.HasPets => request.HasPets,
                MultiplierConditionType.FirstTime => request.IsFirstTime,
                MultiplierConditionType.NoElevator => !request.HasElevator,
                _ => false
            };

            if (applies)
            {
                if (mult.AppliesToTime) timeFactor *= mult.Factor;
                if (mult.AppliesToPrice) priceFactor *= mult.Factor;
            }
        }

        totalMinutes = (int)(totalMinutes * timeFactor);
        totalPrice *= priceFactor;

        // Aplicar descuento por recurrencia
        decimal discountAmount = 0;
        if (request.RecurrenceType != RecurrenceType.None)
        {
            var discount = await _context.RecurrenceDiscounts
                .FirstOrDefaultAsync(d => d.RecurrenceType == request.RecurrenceType && d.IsActive);

            if (discount != null)
            {
                discountAmount = totalPrice * discount.DiscountPercent;
                totalPrice -= discountAmount;
            }
        }

        return Ok(new EstimateResponse
        {
            EstimatedMinutes = totalMinutes,
            FormattedDuration = FormatDuration(totalMinutes),
            Subtotal = totalPrice + discountAmount,
            Discount = discountAmount,
            Total = totalPrice,
            RecurrenceType = request.RecurrenceType,
            DiscountPercent = discountAmount > 0 ? (discountAmount / (totalPrice + discountAmount)) * 100 : 0
        });
    }

    #endregion

    #region Step 4 - Disponibilidad

    /// <summary>
    /// Obtiene los slots de tiempo disponibles para una fecha específica
    /// </summary>
    [HttpPost("availability")]
    public async Task<ActionResult<AvailabilityResponse>> GetAvailability(AvailabilityRequest request)
    {
        // Validar zona de servicio
        var serviceAreaZip = await _context.ServiceAreaZips
            .FirstOrDefaultAsync(z => z.ZipCode == request.ZipCode);

        if (serviceAreaZip == null)
            return BadRequest("ZIP code sin cobertura");

        var serviceAreaId = serviceAreaZip.ServiceAreaId;
        var date = request.Date.Date;
        var dayOfWeek = date.DayOfWeek;

        // Obtener empleadas que cubren esta zona
        var employeesInZone = await _context.EmployeeServiceAreas
            .Where(e => e.ServiceAreaId == serviceAreaId && !e.IsDeleted)
            .Select(e => e.EmployeeId)
            .ToListAsync();

        // Obtener empleadas activas con horario ese día
        var employees = await _context.Employees
            .Where(e => e.IsActive && !e.IsDeleted)
            .Where(e => employeesInZone.Contains(e.Id))
            .Include(e => e.Schedules.Where(s => s.DayOfWeek == dayOfWeek && s.IsAvailable))
            .Include(e => e.TimeOffs.Where(t => 
                t.Status == TimeOffStatus.Approved &&
                t.StartDateTime <= date.AddDays(1) &&
                t.EndDateTime >= date))
            .ToListAsync();

        // Si se requiere equipamiento, filtrar
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

            // Verificar si tiene time-off ese día
            var hasTimeOff = employee.TimeOffs.Any(t =>
                (t.IsAllDay) ||
                (t.StartDateTime.Date <= date && t.EndDateTime.Date >= date));

            if (hasTimeOff) continue;

            // Obtener citas existentes para ese día
            var existingMeetings = await _context.ServiceMeets
                .Where(m => m.AssignedEmployeeId == employee.Id)
                .Where(m => m.ScheduledStart.Date == date)
                .Where(m => m.Status != MeetStatus.Cancelled && m.Status != MeetStatus.NoShow)
                .Select(m => new { m.ScheduledStart, m.ScheduledEnd })
                .ToListAsync();

            // Obtener soft reserves activas
            var activeSoftReserves = await _context.SoftReserves
                .Where(s => s.EmployeeId == employee.Id)
                .Where(s => s.ScheduledStart.Date == date)
                .Where(s => s.Status == SoftReserveStatus.Active && s.ExpiresAt > DateTime.UtcNow)
                .Select(s => new { s.ScheduledStart, s.ScheduledEnd })
                .ToListAsync();

            // Combinar ocupación
            var occupied = existingMeetings
                .Concat(activeSoftReserves)
                .Select(o => (Start: o.ScheduledStart, End: o.ScheduledEnd))
                .ToList();

            // Generar slots cada 30 minutos
            var slotStart = date.Add(schedule.StartTime);
            var dayEnd = date.Add(schedule.EndTime);
            var duration = TimeSpan.FromMinutes(request.EstimatedMinutes);

            while (slotStart.Add(duration) <= dayEnd)
            {
                var slotEnd = slotStart.Add(duration);

                // Verificar si no hay solapamiento
                var hasConflict = occupied.Any(o =>
                    slotStart < o.End && slotEnd > o.Start);

                if (!hasConflict)
                {
                    // Verificar que no exista ya un slot idéntico
                    var existingSlot = slots.FirstOrDefault(s => 
                        s.StartTime == slotStart.TimeOfDay);

                    if (existingSlot != null)
                    {
                        // Agregar empleada al slot existente
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

                slotStart = slotStart.AddMinutes(30); // Incrementar cada 30 min
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

    #region Step 5 - Soft Reserve (anti-colisión)

    /// <summary>
    /// Crea una reserva temporal (soft reserve) mientras el cliente completa el checkout
    /// Usa GET_LOCK de MySQL para evitar colisiones
    /// </summary>
    [HttpPost("soft-reserve")]
    public async Task<ActionResult<SoftReserveResponse>> CreateSoftReserve(CreateSoftReserveRequest request)
    {
        // Obtener ServiceAreaId del ZipCode
        var serviceAreaZip = await _context.ServiceAreaZips
            .Include(z => z.ServiceArea)
            .FirstOrDefaultAsync(z => z.ZipCode == request.ZipCode && z.ServiceArea.IsActive);

        if (serviceAreaZip == null)
        {
            return BadRequest(new { message = "Código postal no tiene cobertura." });
        }

        var serviceAreaId = serviceAreaZip.ServiceAreaId;
        var sessionId = request.SessionId ?? Guid.NewGuid().ToString();
        var lockName = $"softreserve:{request.EmployeeId}:{request.Date:yyyyMMdd}";
        var ttlMinutes = 15;

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Obtener lock con timeout de 3 segundos
            // Nota: GET_LOCK requiere string literal, el lockName se construye de forma segura (solo int + fecha)
            await _context.Database.ExecuteSqlRawAsync(
                "SELECT GET_LOCK(@p0, 3)", lockName);

            var startDateTime = request.Date.Date.Add(request.StartTime);
            var endDateTime = startDateTime.AddMinutes(request.EstimatedMinutes);

            // 1. Verificar que la empleada tenga horario laboral ese día
            var dayOfWeek = request.Date.DayOfWeek;
            var schedule = await _context.EmployeeSchedules
                .FirstOrDefaultAsync(s => 
                    s.EmployeeId == request.EmployeeId &&
                    s.DayOfWeek == dayOfWeek &&
                    s.IsAvailable);

            if (schedule == null)
            {
                await ReleaseLock(lockName);
                return BadRequest(new { message = "Empleada no trabaja en ese día de la semana." });
            }

            // Verificar que el horario esté dentro del turno laboral
            if (request.StartTime < schedule.StartTime || request.StartTime >= schedule.EndTime)
            {
                await ReleaseLock(lockName);
                return BadRequest(new { message = "Horario fuera del turno laboral de la empleada." });
            }

            // 2. Verificar que la empleada no tenga días libres (TimeOff)
            var hasTimeOff = await _context.EmployeeTimeOffs
                .AnyAsync(t => 
                    t.EmployeeId == request.EmployeeId &&
                    t.Status == TimeOffStatus.Approved &&
                    t.StartDateTime <= endDateTime &&
                    t.EndDateTime >= startDateTime);

            if (hasTimeOff)
            {
                await ReleaseLock(lockName);
                return BadRequest(new { message = "Empleada no disponible en esas fechas." });
            }

            // 3. Verificar conflictos con meetings existentes
            var meetingConflict = await _context.ServiceMeets
                .AnyAsync(m =>
                    m.AssignedEmployeeId == request.EmployeeId &&
                    m.Status != MeetStatus.Cancelled && m.Status != MeetStatus.NoShow &&
                    m.ScheduledStart < endDateTime && m.ScheduledEnd > startDateTime);

            if (meetingConflict)
            {
                await ReleaseLock(lockName);
                return Conflict(new { message = "Este horario ya no está disponible." });
            }

            // 4. Verificar conflictos con soft reserves activas
            var softReserveConflict = await _context.SoftReserves
                .AnyAsync(s =>
                    s.EmployeeId == request.EmployeeId &&
                    s.Status == SoftReserveStatus.Active &&
                    s.ExpiresAt > DateTime.UtcNow &&
                    s.ScheduledStart < endDateTime && s.ScheduledEnd > startDateTime);

            if (softReserveConflict)
            {
                await ReleaseLock(lockName);
                return Conflict(new { message = "Alguien más está reservando este horario. Intenta otro." });
            }

            // Crear soft reserve
            var softReserve = new SoftReserve
            {
                SessionId = sessionId,
                CustomerId = request.CustomerId,
                EmployeeId = request.EmployeeId,
                ServiceAreaId = serviceAreaId,
                ScheduledStart = startDateTime,
                ScheduledEnd = endDateTime,
                ExpiresAt = DateTime.UtcNow.AddMinutes(ttlMinutes),
                Status = SoftReserveStatus.Active
            };

            _context.SoftReserves.Add(softReserve);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Liberar lock
            await ReleaseLock(lockName);

            return Ok(new SoftReserveResponse
            {
                SoftReserveId = softReserve.Id,
                SessionId = sessionId,
                ScheduledStart = startDateTime,
                ScheduledEnd = endDateTime,
                ExpiresAt = softReserve.ExpiresAt,
                TtlSeconds = ttlMinutes * 60,
                Message = $"Horario reservado por {ttlMinutes} minutos. Complete el pago para confirmar."
            });
        }
        catch (Exception ex)
        {
            await ReleaseLock(lockName);
            _logger.LogError(ex, "Error creating soft reserve");
            throw;
        }
    }

    /// <summary>
    /// Cancela una soft reserve manualmente
    /// </summary>
    [HttpDelete("soft-reserve/{id}")]
    public async Task<IActionResult> CancelSoftReserve(int id, [FromQuery] string sessionId)
    {
        var softReserve = await _context.SoftReserves
            .FirstOrDefaultAsync(s => s.Id == id && s.SessionId == sessionId);

        if (softReserve == null)
            return NotFound();

        if (softReserve.Status != SoftReserveStatus.Active)
            return BadRequest("La reserva ya fue procesada o expiró.");

        softReserve.Status = SoftReserveStatus.Cancelled;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Extiende el tiempo de una soft reserve
    /// </summary>
    [HttpPost("soft-reserve/{id}/extend")]
    public async Task<ActionResult<SoftReserveResponse>> ExtendSoftReserve(int id, [FromQuery] string sessionId)
    {
        var softReserve = await _context.SoftReserves
            .FirstOrDefaultAsync(s => s.Id == id && s.SessionId == sessionId);

        if (softReserve == null)
            return NotFound();

        if (softReserve.Status != SoftReserveStatus.Active || softReserve.ExpiresAt <= DateTime.UtcNow)
            return BadRequest("La reserva ya expiró.");

        // Extender 10 minutos más desde la expiración actual (no desde ahora)
        var newExpiry = softReserve.ExpiresAt > DateTime.UtcNow 
            ? softReserve.ExpiresAt.AddMinutes(10) 
            : DateTime.UtcNow.AddMinutes(10);
        softReserve.ExpiresAt = newExpiry;
        await _context.SaveChangesAsync();

        return Ok(new SoftReserveResponse
        {
            SoftReserveId = softReserve.Id,
            SessionId = softReserve.SessionId,
            ScheduledStart = softReserve.ScheduledStart,
            ScheduledEnd = softReserve.ScheduledEnd,
            ExpiresAt = softReserve.ExpiresAt,
            TtlSeconds = (int)(softReserve.ExpiresAt - DateTime.UtcNow).TotalSeconds,
            Message = "Reserva extendida."
        });
    }

    #endregion

    #region Step 6 - Confirm Booking

    /// <summary>
    /// Confirma la reserva y crea la orden + cita
    /// </summary>
    [HttpPost("confirm")]
    public async Task<ActionResult<BookingConfirmationResponse>> ConfirmBooking(ConfirmBookingRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Validar soft reserve
            var softReserve = await _context.SoftReserves
                .FirstOrDefaultAsync(s => s.Id == request.SoftReserveId && s.SessionId == request.SessionId);

            if (softReserve == null)
                return NotFound("Reserva no encontrada.");

            if (softReserve.Status != SoftReserveStatus.Active)
                return BadRequest("La reserva ya fue procesada.");

            if (softReserve.ExpiresAt <= DateTime.UtcNow)
            {
                softReserve.Status = SoftReserveStatus.Expired;
                await _context.SaveChangesAsync();
                return BadRequest("La reserva expiró. Por favor selecciona otro horario.");
            }

            // CRÍTICO: Re-calcular pricing en backend para prevenir fraude
            _logger.LogInformation("Re-calculating pricing for confirmation - SoftReserve {SoftReserveId}", request.SoftReserveId);
            
            var serviceType = await _context.ServiceTypes.FindAsync(request.ServiceTypeId);
            if (serviceType == null)
                return BadRequest("Tipo de servicio no válido");

            decimal calculatedSubtotal = serviceType.Price;
            int calculatedMinutes = serviceType.EstimatedMinutes;

            // Agregar servicios adicionales
            if (request.AdditionalServiceIds?.Any() == true)
            {
                var additionals = await _context.AdditionalServiceTypes
                    .Where(a => request.AdditionalServiceIds.Contains(a.Id))
                    .ToListAsync();
                
                calculatedSubtotal += additionals.Sum(a => a.Price);
                calculatedMinutes += additionals.Sum(a => a.AdditionalMinutes);
            }

            // Aplicar multiplicadores (pets, dirt level, etc.)
            var multipliers = await _context.PriceMultipliers
                .Where(m => m.IsActive)
                .Where(m => m.ServiceTypeId == null || m.ServiceTypeId == request.ServiceTypeId)
                .ToListAsync();

            decimal priceFactor = 1.0m;
            foreach (var mult in multipliers)
            {
                bool applies = mult.ConditionType switch
                {
                    MultiplierConditionType.HasPets => request.HasPets,
                    MultiplierConditionType.DirtLevel => (int)request.DirtLevel == (int)(mult.MinValue ?? 1),
                    MultiplierConditionType.NoElevator => !request.HasElevator,
                    _ => false
                };

                if (applies && mult.AppliesToPrice)
                    priceFactor *= mult.Factor;
            }

            calculatedSubtotal *= priceFactor;

            // Aplicar descuento por recurrencia
            decimal calculatedDiscount = 0;
            if (request.RecurrenceType != RecurrenceType.None)
            {
                var discount = await _context.RecurrenceDiscounts
                    .FirstOrDefaultAsync(d => d.RecurrenceType == request.RecurrenceType && d.IsActive);
                if (discount != null)
                {
                    calculatedDiscount = calculatedSubtotal * discount.DiscountPercent;
                }
            }

            decimal calculatedTotal = calculatedSubtotal - calculatedDiscount;

            // Validar que el total enviado por el cliente coincida (con margen de error de $0.01)
            if (Math.Abs(request.Total - calculatedTotal) > 0.01m)
            {
                _logger.LogWarning("Price mismatch detected - Expected: {Expected}, Received: {Received}", 
                    calculatedTotal, request.Total);
                return BadRequest($"El total no coincide. Esperado: ${calculatedTotal:F2}, Recibido: ${request.Total:F2}");
            }

            // NUEVO: Crear usuario automáticamente si no existe
            string? customerId = request.CustomerId;
            AuthToken? authToken = null;
            bool isNewUser = false;

            if (string.IsNullOrEmpty(customerId))
            {
                // Verificar si el email ya existe
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.ContactEmail);
                
                if (existingUser != null)
                {
                    // Usuario existe pero no está logueado - debería loguearse
                    return BadRequest(new { 
                        message = "Este email ya está registrado. Por favor inicia sesión para continuar.",
                        requireLogin = true
                    });
                }

                // Validar que se proporcionó contraseña para crear cuenta
                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new { message = "Se requiere una contraseña para crear tu cuenta." });
                }

                // Crear nuevo usuario
                var newUser = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = request.ContactEmail,
                    NormalizedUserName = request.ContactEmail.ToUpperInvariant(),
                    Email = request.ContactEmail,
                    NormalizedEmail = request.ContactEmail.ToUpperInvariant(),
                    EmailConfirmed = false,
                    PhoneNumber = request.ContactPhone,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    FirstName = request.ContactName?.Split(' ').FirstOrDefault(),
                    LastName = request.ContactName?.Split(' ').Skip(1).FirstOrDefault()
                };

                newUser.PasswordHash = _passwordHasher.HashPassword(newUser, request.Password);
                _context.Users.Add(newUser);

                // Asignar rol Customer
                var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == Roles.Customer);
                if (customerRole != null)
                {
                    _context.UserRoles.Add(new IdentityUserRole<string>
                    {
                        UserId = newUser.Id,
                        RoleId = customerRole.Id
                    });
                }

                customerId = newUser.Id;
                isNewUser = true;

                // Generar tokens JWT para auto-login
                var roles = new[] { Roles.Customer };
                var accessToken = _jwtService.GenerateAccessToken(newUser.Id, newUser.Email!, roles);
                var refreshToken = _jwtService.GenerateRefreshToken();

                authToken = new AuthToken
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    IsNewUser = true
                };

                _logger.LogInformation("Usuario creado automáticamente durante booking: {Email}", request.ContactEmail);
            }

            // Crear orden con pricing validado
            var order = new ServiceOrder
            {
                CustomerId = customerId,
                ServiceAreaId = softReserve.ServiceAreaId,
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
                Subtotal = calculatedSubtotal,
                Tax = 0, // No tax in MVP
                Discount = calculatedDiscount,
                Total = calculatedTotal,
                OrderStatus = OrderStatus.Draft, // MVP: Admin confirms manually
                RecurrenceType = request.RecurrenceType,
                RecurrenceEndDate = request.RecurrenceEndDate,
                Source = OrderSource.Website,
                ContactName = request.ContactName,
                ContactPhone = request.ContactPhone,
                ContactEmail = request.ContactEmail,
                SpecialInstructions = request.SpecialInstructions,
                PreferredStartTime = softReserve.ScheduledStart.TimeOfDay,
                EstimatedDurationMinutes = (int)(softReserve.ScheduledEnd - softReserve.ScheduledStart).TotalMinutes
            };

            _context.ServiceOrders.Add(order);
            await _context.SaveChangesAsync();

            // Agregar items adicionales
            if (request.AdditionalServiceIds?.Any() == true)
            {
                foreach (var additionalId in request.AdditionalServiceIds)
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

            // Crear cita (ServiceMeet)
            var meet = new ServiceMeet
            {
                ServiceOrderId = order.Id,
                AssignedEmployeeId = softReserve.EmployeeId,
                ServiceAreaId = softReserve.ServiceAreaId,
                ScheduledStart = softReserve.ScheduledStart,
                ScheduledEnd = softReserve.ScheduledEnd,
                EstimatedDurationMinutes = (int)(softReserve.ScheduledEnd - softReserve.ScheduledStart).TotalMinutes,
                Status = MeetStatus.Scheduled // MVP: Admin confirms and assigns
            };

            _context.ServiceMeets.Add(meet);

            // Actualizar soft reserve
            softReserve.Status = SoftReserveStatus.Converted;
            softReserve.ServiceOrderId = order.Id;
            softReserve.CustomerId = customerId;

            await _context.SaveChangesAsync();

            // Si es recurrente, crear citas futuras (se crean en estado Scheduled)
            if (request.RecurrenceType != RecurrenceType.None)
            {
                await CreateRecurringMeetings(order, meet, request.RecurrenceType, request.RecurrenceEndDate);
            }

            await transaction.CommitAsync();

            _logger.LogInformation("Order confirmed - OrderId: {OrderId}, MeetId: {MeetId}, SessionId: {SessionId}, Total: {Total}", 
                order.Id, meet.Id, request.SessionId, order.Total);

            // Enviar email de confirmación
            if (!string.IsNullOrEmpty(request.ContactEmail))
            {
                var employee = await _context.Employees.FindAsync(softReserve.EmployeeId);
                var serviceTypeName = serviceType?.Name ?? "Cleaning Service";

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendBookingConfirmationAsync(
                            request.ContactEmail,
                            new BookingConfirmationEmail(
                                CustomerName: request.ContactName ?? "Valued Customer",
                                ServiceType: serviceTypeName,
                                ScheduledDate: meet.ScheduledStart.Date,
                                ScheduledTime: meet.ScheduledStart.ToString("h:mm tt"),
                                Address: $"{request.Address}, {request.City}, {request.State} {request.ZipCode}",
                                TotalAmount: order.Total,
                                EmployeeName: employee != null ? $"{employee.FirstName} {employee.LastName}" : "Our professional cleaner",
                                EstimatedDuration: meet.EstimatedDurationMinutes
                            ));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send confirmation email for Order {OrderId}", order.Id);
                    }
                });
            }

            return Ok(new BookingConfirmationResponse
            {
                OrderId = order.Id,
                MeetId = meet.Id,
                ConfirmationNumber = $"SBM-{order.Id:D6}",
                ScheduledStart = meet.ScheduledStart,
                ScheduledEnd = meet.ScheduledEnd,
                Total = order.Total,
                OrderStatus = order.OrderStatus.ToString(),
                Message = isNewUser 
                    ? "¡Cuenta creada y reserva confirmada! Ahora puedes dar seguimiento a tu servicio."
                    : "Your booking request has been received! We'll confirm your appointment shortly.",
                AuthToken = authToken
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

    private async Task ReleaseLock(string lockName)
    {
        // lockName se construye de forma segura (employeeId:fecha)
        await _context.Database.ExecuteSqlRawAsync(
            "SELECT RELEASE_LOCK(@p0)", lockName);
    }

    private async Task CreateRecurringMeetings(ServiceOrder order, ServiceMeet firstMeet, 
        RecurrenceType recurrenceType, DateTime? endDate)
    {
        var maxOccurrences = 8; // Horizonte de 8 semanas
        var interval = recurrenceType switch
        {
            RecurrenceType.Weekly => 7,
            RecurrenceType.BiWeekly => 14,
            RecurrenceType.Monthly => 30,
            _ => 0
        };

        if (interval == 0) return;

        var currentStart = firstMeet.ScheduledStart.AddDays(interval);
        var horizon = endDate ?? DateTime.UtcNow.AddDays(maxOccurrences * interval);
        var count = 0;
        var duration = firstMeet.EstimatedDurationMinutes;

        _logger.LogInformation("Creating recurring meetings for Order {OrderId}, type {RecurrenceType}", 
            order.Id, recurrenceType);

        while (currentStart <= horizon && count < maxOccurrences)
        {
            var currentEnd = currentStart.AddMinutes(duration);

            // Validar que la empleada no tenga conflictos en esta fecha futura
            var hasConflict = await _context.ServiceMeets
                .AnyAsync(m =>
                    m.AssignedEmployeeId == firstMeet.AssignedEmployeeId &&
                    m.Status != MeetStatus.Cancelled && m.Status != MeetStatus.NoShow &&
                    m.ScheduledStart < currentEnd && m.ScheduledEnd > currentStart);

            // Validar TimeOff
            var hasTimeOff = await _context.EmployeeTimeOffs
                .AnyAsync(t =>
                    t.EmployeeId == firstMeet.AssignedEmployeeId &&
                    t.Status == TimeOffStatus.Approved &&
                    t.StartDateTime <= currentEnd &&
                    t.EndDateTime >= currentStart);

            // Solo crear la cita si no hay conflicto
            if (!hasConflict && !hasTimeOff)
            {
                _context.ServiceMeets.Add(new ServiceMeet
                {
                    ServiceOrderId = order.Id,
                    AssignedEmployeeId = firstMeet.AssignedEmployeeId,
                    ServiceAreaId = firstMeet.ServiceAreaId,
                    ScheduledStart = currentStart,
                    ScheduledEnd = currentEnd,
                    EstimatedDurationMinutes = firstMeet.EstimatedDurationMinutes,
                    Status = MeetStatus.Scheduled
                });
                count++;
            }
            else
            {
                _logger.LogWarning("Skipping recurring meeting at {Date} due to conflict or TimeOff", currentStart);
            }

            currentStart = currentStart.AddDays(interval);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Created {Count} recurring meetings for Order {OrderId}", count, order.Id);
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
    [Required(ErrorMessage = "El tipo de servicio es requerido")]
    [Range(1, int.MaxValue, ErrorMessage = "ID de tipo de servicio inválido")]
    public int ServiceTypeId { get; init; }
    
    public int? CleaningPlaceId { get; init; }
    public List<RoomSelection> Rooms { get; init; } = new();
    public List<int> AdditionalServiceIds { get; init; } = new();
    
    [Range(0, 50000, ErrorMessage = "Los pies cuadrados deben estar entre 0 y 50,000")]
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
    [Required(ErrorMessage = "El código postal es requerido")]
    [StringLength(10, MinimumLength = 5, ErrorMessage = "El código postal debe tener entre 5 y 10 caracteres")]
    [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Formato de código postal inválido")]
    public string ZipCode { get; init; } = "";
    
    [Required(ErrorMessage = "La fecha es requerida")]
    public DateTime Date { get; init; }
    
    [Required(ErrorMessage = "La duración estimada es requerida")]
    [Range(30, 480, ErrorMessage = "La duración debe estar entre 30 y 480 minutos")]
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
    
    [Required(ErrorMessage = "El empleado es requerido")]
    [Range(1, int.MaxValue, ErrorMessage = "ID de empleado inválido")]
    public int EmployeeId { get; init; }
    
    [Required(ErrorMessage = "El código postal es requerido")]
    [StringLength(10, MinimumLength = 5)]
    public string ZipCode { get; init; } = "";
    
    [Required(ErrorMessage = "La fecha es requerida")]
    public DateTime Date { get; init; }
    
    [Required(ErrorMessage = "La hora de inicio es requerida")]
    public TimeSpan StartTime { get; init; }
    
    [Required(ErrorMessage = "La duración estimada es requerida")]
    [Range(30, 480, ErrorMessage = "La duración debe estar entre 30 y 480 minutos")]
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
    [Required(ErrorMessage = "La reserva es requerida")]
    [Range(1, int.MaxValue)]
    public int SoftReserveId { get; init; }
    
    [Required(ErrorMessage = "El ID de sesión es requerido")]
    [StringLength(100, MinimumLength = 10)]
    public string SessionId { get; init; } = "";
    
    [StringLength(100)]
    public string? CustomerId { get; init; }
    
    public bool PaymentConfirmed { get; init; }
    
    // Dirección
    [Required(ErrorMessage = "El código postal es requerido")]
    [StringLength(10, MinimumLength = 5)]
    public string ZipCode { get; init; } = "";
    
    [Required(ErrorMessage = "La dirección es requerida")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "La dirección debe tener entre 5 y 500 caracteres")]
    public string Address { get; init; } = "";
    
    [StringLength(200)]
    public string? AddressLine2 { get; init; }
    
    [StringLength(100)]
    public string? City { get; init; }
    
    [StringLength(50)]
    public string? State { get; init; }
    
    // Servicio
    [Required(ErrorMessage = "El tipo de servicio es requerido")]
    [Range(1, int.MaxValue)]
    public int ServiceTypeId { get; init; }
    
    public int? CleaningPlaceId { get; init; }
    
    [Range(0, 20, ErrorMessage = "Número de recámaras inválido")]
    public int Bedrooms { get; init; } = 1;
    
    [Range(0, 20, ErrorMessage = "Número de baños inválido")]
    public int Bathrooms { get; init; } = 1;
    
    [Range(0, 50000)]
    public int? SquareFootage { get; init; }
    
    public DirtLevel DirtLevel { get; init; } = DirtLevel.Normal;
    public bool HasPets { get; init; }
    
    [Range(0, 100)]
    public int? FloorLevel { get; init; }
    
    public bool HasElevator { get; init; } = true;
    public List<int>? AdditionalServiceIds { get; init; }
    
    // Montos
    [Range(0, 100000)]
    public decimal Subtotal { get; init; }
    
    [Range(0, 100000)]
    public decimal Tax { get; init; }
    
    [Range(0, 100000)]
    public decimal Discount { get; init; }
    
    [Range(0, 100000)]
    public decimal Total { get; init; }
    
    // Recurrencia
    public RecurrenceType RecurrenceType { get; init; } = RecurrenceType.None;
    public DateTime? RecurrenceEndDate { get; init; }
    
    // Contacto
    [StringLength(100)]
    public string? ContactName { get; init; }
    
    [Phone(ErrorMessage = "Formato de teléfono inválido")]
    [StringLength(20)]
    public string? ContactPhone { get; init; }
    
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Formato de email inválido")]
    [StringLength(256)]
    public string ContactEmail { get; init; } = "";
    
    // Password para crear cuenta si no existe
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
}

public record AuthToken
{
    public string AccessToken { get; init; } = "";
    public string RefreshToken { get; init; } = "";
    public DateTime ExpiresAt { get; init; }
    public bool IsNewUser { get; init; }
}

#endregion
