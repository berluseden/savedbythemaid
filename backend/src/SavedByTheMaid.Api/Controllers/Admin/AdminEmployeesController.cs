using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Api.Auth;
using SavedByTheMaid.Api.Extensions;
using SavedByTheMaid.Application.DTOs.Admin;
using SavedByTheMaid.Infrastructure.Data;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Api.Controllers;

[ApiController]
[Route("api/admin/employees")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminEmployeesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminEmployeesController> _logger;
    private readonly IValidator<CreateEmployeeRequest> _createValidator;
    private readonly IValidator<UpdateEmployeeRequest> _updateValidator;
    private readonly IValidator<CreateEmployeeScheduleRequest> _scheduleValidator;
    private readonly IValidator<CreateEmployeeTimeOffRequest> _timeOffValidator;

    public AdminEmployeesController(
        ApplicationDbContext context,
        ILogger<AdminEmployeesController> logger,
        IValidator<CreateEmployeeRequest> createValidator,
        IValidator<UpdateEmployeeRequest> updateValidator,
        IValidator<CreateEmployeeScheduleRequest> scheduleValidator,
        IValidator<CreateEmployeeTimeOffRequest> timeOffValidator)
    {
        _context = context;
        _logger = logger;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _scheduleValidator = scheduleValidator;
        _timeOffValidator = timeOffValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EmployeeAdminDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<EmployeeAdminDto>>> GetAll(
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Employees.Where(e => !e.IsDeleted);

        if (isActive.HasValue)
            query = query.Where(e => e.IsActive == isActive.Value);

        return await query
            .AsNoTracking()
            .Include(e => e.PrimaryServiceArea)
            .Include(e => e.ServiceAreas)
                .ThenInclude(sa => sa.ServiceArea)
            .Select(e => new EmployeeAdminDto
            {
                Id = e.Id,
                FirstName = e.FirstName,
                LastName = e.LastName,
                Email = e.Email,
                Phone = e.Phone,
                IsActive = e.IsActive,
                PrimaryServiceAreaName = e.PrimaryServiceArea != null ? e.PrimaryServiceArea.Name : null,
                ServiceAreaCount = e.ServiceAreas.Count,
                MaxDailyHours = e.MaxDailyHours,
                MaxDailyServices = e.MaxDailyServices,
                HasActiveTimeOff = e.TimeOffs.Any(t => t.Status == TimeOffStatus.Approved && t.EndDateTime >= DateTime.UtcNow && !t.IsDeleted)
            })
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EmployeeDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeDetailDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .Include(e => e.PrimaryServiceArea)
            .Include(e => e.ServiceAreas).ThenInclude(sa => sa.ServiceArea)
            .Include(e => e.Schedules)
            .Include(e => e.Equipment).ThenInclude(eq => eq.Equipment)
            .Include(e => e.TimeOffs.Where(t => t.EndDateTime >= DateTime.UtcNow))
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);

        if (employee == null)
            return NotFound();

        return new EmployeeDetailDto
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            Phone = employee.Phone,
            Address = employee.Address,
            IsActive = employee.IsActive,
            MaxDailyHours = employee.MaxDailyHours,
            MaxDailyServices = employee.MaxDailyServices,
            PrimaryServiceAreaId = employee.PrimaryServiceAreaId,
            PrimaryServiceAreaName = employee.PrimaryServiceArea?.Name,
            ServiceAreas = employee.ServiceAreas.Select(sa => new EmployeeServiceAreaDto
            {
                ServiceAreaId = sa.ServiceAreaId,
                ServiceAreaName = sa.ServiceArea?.Name ?? "",
                IsPrimary = sa.IsPrimary
            }).ToList(),
            Schedules = employee.Schedules.Where(s => !s.IsDeleted).Select(s => new EmployeeScheduleDto
            {
                Id = s.Id,
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                BufferMinutes = s.BufferMinutes,
                IsAvailable = s.IsAvailable
            }).ToList(),
            TimeOffs = employee.TimeOffs.Where(t => !t.IsDeleted).Select(t => new EmployeeTimeOffDto
            {
                Id = t.Id,
                StartDateTime = t.StartDateTime,
                EndDateTime = t.EndDateTime,
                IsAllDay = t.IsAllDay,
                Type = t.Type,
                Reason = t.Reason,
                Status = t.Status
            }).ToList(),
            Equipment = employee.Equipment.Select(eq => new EmployeeEquipmentDto
            {
                EquipmentId = eq.EquipmentId,
                EquipmentName = eq.Equipment?.Name ?? "",
                IsAvailable = eq.IsAvailable
            }).ToList()
        };
    }

    [HttpPost]
    [ProducesResponseType(typeof(EmployeeDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EmployeeDetailDto>> Create(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = await _createValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var employee = new Employee
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            PrimaryServiceAreaId = request.PrimaryServiceAreaId,
            MaxDailyHours = request.MaxDailyHours,
            MaxDailyServices = request.MaxDailyServices,
            IsActive = true
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Employee created with ID {EmployeeId}", employee.Id);

        var dto = new EmployeeDetailDto
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            Phone = employee.Phone,
            Address = employee.Address,
            IsActive = employee.IsActive,
            MaxDailyHours = employee.MaxDailyHours,
            MaxDailyServices = employee.MaxDailyServices,
            PrimaryServiceAreaId = employee.PrimaryServiceAreaId
        };

        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, dto);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = await _updateValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var employee = await _context.Employees.FindAsync(new object[] { id }, cancellationToken);
        if (employee == null || employee.IsDeleted) return NotFound();

        employee.FirstName = request.FirstName;
        employee.LastName = request.LastName;
        employee.Email = request.Email;
        employee.Phone = request.Phone;
        employee.Address = request.Address;
        employee.PrimaryServiceAreaId = request.PrimaryServiceAreaId;
        employee.MaxDailyHours = request.MaxDailyHours;
        employee.MaxDailyServices = request.MaxDailyServices;
        employee.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FindAsync(new object[] { id }, cancellationToken);
        if (employee == null || employee.IsDeleted)
            return NoContent(); // Idempotent: already deleted or never existed

        employee.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // Service Areas
    [HttpPost("{id}/service-areas/{serviceAreaId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddServiceArea(
        int id,
        int serviceAreaId,
        [FromQuery] bool isPrimary = false,
        CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FindAsync(new object[] { id }, cancellationToken);
        var serviceArea = await _context.ServiceAreas.FindAsync(new object[] { serviceAreaId }, cancellationToken);

        if (employee == null || serviceArea == null) return NotFound();

        var existing = await _context.EmployeeServiceAreas
            .FirstOrDefaultAsync(e => e.EmployeeId == id && e.ServiceAreaId == serviceAreaId, cancellationToken);

        if (existing != null)
        {
            existing.IsPrimary = isPrimary;
        }
        else
        {
            _context.EmployeeServiceAreas.Add(new EmployeeServiceArea
            {
                EmployeeId = id,
                ServiceAreaId = serviceAreaId,
                IsPrimary = isPrimary
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}/service-areas/{serviceAreaId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveServiceArea(
        int id,
        int serviceAreaId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.EmployeeServiceAreas
            .FirstOrDefaultAsync(e => e.EmployeeId == id && e.ServiceAreaId == serviceAreaId, cancellationToken);

        if (existing == null) return NotFound();

        _context.EmployeeServiceAreas.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // Equipment
    [HttpPost("{id}/equipment/{equipmentId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddEquipment(
        int id,
        int equipmentId,
        [FromQuery] bool isAvailable = true,
        CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FindAsync(new object[] { id }, cancellationToken);
        var equipment = await _context.Equipment.FindAsync(new object[] { equipmentId }, cancellationToken);

        if (employee == null || equipment == null) return NotFound();

        var existing = await _context.EmployeeEquipment
            .FirstOrDefaultAsync(e => e.EmployeeId == id && e.EquipmentId == equipmentId, cancellationToken);

        if (existing != null)
        {
            existing.IsAvailable = isAvailable;
        }
        else
        {
            _context.EmployeeEquipment.Add(new EmployeeEquipment
            {
                EmployeeId = id,
                EquipmentId = equipmentId,
                IsAvailable = isAvailable
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}/equipment/{equipmentId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveEquipment(
        int id,
        int equipmentId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.EmployeeEquipment
            .FirstOrDefaultAsync(e => e.EmployeeId == id && e.EquipmentId == equipmentId, cancellationToken);

        if (existing == null) return NotFound();

        _context.EmployeeEquipment.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // Time Off
    [HttpGet("{id}/time-off")]
    [ProducesResponseType(typeof(IEnumerable<EmployeeTimeOffDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<EmployeeTimeOffDto>>> GetTimeOffs(
        int id,
        [FromQuery] DateTime? from = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.EmployeeTimeOffs
            .AsNoTracking()
            .Where(t => t.EmployeeId == id && !t.IsDeleted);

        if (from.HasValue)
            query = query.Where(t => t.EndDateTime >= from.Value);

        return await query
            .OrderBy(t => t.StartDateTime)
            .Select(t => new EmployeeTimeOffDto
            {
                Id = t.Id,
                StartDateTime = t.StartDateTime,
                EndDateTime = t.EndDateTime,
                IsAllDay = t.IsAllDay,
                Type = t.Type,
                Reason = t.Reason,
                Status = t.Status
            })
            .ToListAsync(cancellationToken);
    }

    [HttpPost("{id}/time-off")]
    [ProducesResponseType(typeof(EmployeeTimeOffDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeTimeOffDto>> AddTimeOff(
        int id,
        CreateEmployeeTimeOffRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = await _timeOffValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var employee = await _context.Employees.FindAsync(new object[] { id }, cancellationToken);
        if (employee == null || employee.IsDeleted) return NotFound();

        var timeOff = new EmployeeTimeOff
        {
            EmployeeId = id,
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime,
            IsAllDay = request.IsAllDay,
            Type = request.Type,
            Reason = request.Reason,
            Status = TimeOffStatus.Approved // Auto-approved from admin
        };

        _context.EmployeeTimeOffs.Add(timeOff);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Time off added for employee {EmployeeId}, ID {TimeOffId}", id, timeOff.Id);

        var dto = new EmployeeTimeOffDto
        {
            Id = timeOff.Id,
            StartDateTime = timeOff.StartDateTime,
            EndDateTime = timeOff.EndDateTime,
            IsAllDay = timeOff.IsAllDay,
            Type = timeOff.Type,
            Reason = timeOff.Reason,
            Status = timeOff.Status
        };

        return CreatedAtAction(nameof(GetTimeOffs), new { id }, dto);
    }

    [HttpDelete("{id}/time-off/{timeOffId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTimeOff(
        int id,
        int timeOffId,
        CancellationToken cancellationToken = default)
    {
        var timeOff = await _context.EmployeeTimeOffs
            .FirstOrDefaultAsync(t => t.Id == timeOffId && t.EmployeeId == id, cancellationToken);

        if (timeOff == null || timeOff.IsDeleted)
            return NoContent(); // Idempotent

        timeOff.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // Schedules
    [HttpGet("{id}/schedules")]
    [ProducesResponseType(typeof(IEnumerable<EmployeeScheduleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EmployeeScheduleDto>>> GetSchedules(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeSchedules
            .AsNoTracking()
            .Where(s => s.EmployeeId == id && !s.IsDeleted)
            .OrderBy(s => s.DayOfWeek)
            .Select(s => new EmployeeScheduleDto
            {
                Id = s.Id,
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                BufferMinutes = s.BufferMinutes,
                IsAvailable = s.IsAvailable
            })
            .ToListAsync(cancellationToken);
    }

    [HttpPost("{id}/schedules")]
    [ProducesResponseType(typeof(EmployeeScheduleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeScheduleDto>> AddSchedule(
        int id,
        CreateEmployeeScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = await _scheduleValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var employee = await _context.Employees.FindAsync(new object[] { id }, cancellationToken);
        if (employee == null || employee.IsDeleted) return NotFound();

        EmployeeSchedule schedule;

        // Upsert: if a schedule exists for that day, update it
        var existing = await _context.EmployeeSchedules
            .FirstOrDefaultAsync(s => s.EmployeeId == id && s.DayOfWeek == request.DayOfWeek && !s.IsDeleted, cancellationToken);

        if (existing != null)
        {
            existing.StartTime = request.StartTime;
            existing.EndTime = request.EndTime;
            existing.BufferMinutes = request.BufferMinutes;
            existing.IsAvailable = request.IsAvailable;
            schedule = existing;
        }
        else
        {
            schedule = new EmployeeSchedule
            {
                EmployeeId = id,
                DayOfWeek = request.DayOfWeek,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                BufferMinutes = request.BufferMinutes,
                IsAvailable = request.IsAvailable
            };
            _context.EmployeeSchedules.Add(schedule);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new EmployeeScheduleDto
        {
            Id = schedule.Id,
            DayOfWeek = schedule.DayOfWeek,
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime,
            BufferMinutes = schedule.BufferMinutes,
            IsAvailable = schedule.IsAvailable
        };

        return CreatedAtAction(nameof(GetSchedules), new { id }, dto);
    }

    [HttpDelete("{id}/schedules/{dayOfWeek}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSchedule(
        int id,
        DayOfWeek dayOfWeek,
        CancellationToken cancellationToken = default)
    {
        var schedule = await _context.EmployeeSchedules
            .FirstOrDefaultAsync(s => s.EmployeeId == id && s.DayOfWeek == dayOfWeek, cancellationToken);

        if (schedule == null || schedule.IsDeleted)
            return NoContent(); // Idempotent

        schedule.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

// DTOs
public record EmployeeAdminDto
{
    public int Id { get; init; }
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public bool IsActive { get; init; }
    public string? PrimaryServiceAreaName { get; init; }
    public int ServiceAreaCount { get; init; }
    public int? MaxDailyHours { get; init; }
    public int? MaxDailyServices { get; init; }
    public bool HasActiveTimeOff { get; init; }
}

public record EmployeeDetailDto
{
    public int Id { get; init; }
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public bool IsActive { get; init; }
    public int? MaxDailyHours { get; init; }
    public int? MaxDailyServices { get; init; }
    public int? PrimaryServiceAreaId { get; init; }
    public string? PrimaryServiceAreaName { get; init; }
    public List<EmployeeServiceAreaDto> ServiceAreas { get; init; } = new();
    public List<EmployeeScheduleDto> Schedules { get; init; } = new();
    public List<EmployeeTimeOffDto> TimeOffs { get; init; } = new();
    public List<EmployeeEquipmentDto> Equipment { get; init; } = new();
}

public record EmployeeServiceAreaDto
{
    public int ServiceAreaId { get; init; }
    public string ServiceAreaName { get; init; } = "";
    public bool IsPrimary { get; init; }
}

public record EmployeeScheduleDto
{
    public int Id { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public int BufferMinutes { get; init; }
    public bool IsAvailable { get; init; }
}

public record EmployeeTimeOffDto
{
    public int Id { get; init; }
    public DateTime StartDateTime { get; init; }
    public DateTime EndDateTime { get; init; }
    public bool IsAllDay { get; init; }
    public TimeOffType Type { get; init; }
    public string? Reason { get; init; }
    public TimeOffStatus Status { get; init; }
}

public record EmployeeEquipmentDto
{
    public int EquipmentId { get; init; }
    public string EquipmentName { get; init; } = "";
    public bool IsAvailable { get; init; }
}

// Request records are defined in SavedByTheMaid.Application.DTOs.Admin
