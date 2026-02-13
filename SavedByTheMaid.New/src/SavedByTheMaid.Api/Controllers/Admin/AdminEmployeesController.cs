using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Api.Auth;
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

    public AdminEmployeesController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeAdminDto>>> GetAll([FromQuery] bool? isActive = null)
    {
        var query = _context.Employees.Where(e => !e.IsDeleted);
        
        if (isActive.HasValue)
            query = query.Where(e => e.IsActive == isActive.Value);

        return await query
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
                HasActiveTimeOff = e.TimeOffs.Any(t => t.Status == TimeOffStatus.Approved && t.EndDateTime >= DateTime.UtcNow)
            })
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Employee>> GetById(int id)
    {
        var employee = await _context.Employees
            .Include(e => e.PrimaryServiceArea)
            .Include(e => e.ServiceAreas).ThenInclude(sa => sa.ServiceArea)
            .Include(e => e.Schedules)
            .Include(e => e.Equipment).ThenInclude(eq => eq.Equipment)
            .Include(e => e.TimeOffs.Where(t => t.EndDateTime >= DateTime.UtcNow))
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        return employee == null ? NotFound() : employee;
    }

    [HttpPost]
    public async Task<ActionResult<Employee>> Create(CreateEmployeeAdminRequest request)
    {
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
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, employee);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateEmployeeAdminRequest request)
    {
        var employee = await _context.Employees.FindAsync(id);
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

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null) return NotFound();

        employee.IsDeleted = true;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Service Areas
    [HttpPost("{id}/service-areas/{serviceAreaId}")]
    public async Task<IActionResult> AddServiceArea(int id, int serviceAreaId, [FromQuery] bool isPrimary = false)
    {
        var employee = await _context.Employees.FindAsync(id);
        var serviceArea = await _context.ServiceAreas.FindAsync(serviceAreaId);

        if (employee == null || serviceArea == null) return NotFound();

        var existing = await _context.EmployeeServiceAreas
            .FirstOrDefaultAsync(e => e.EmployeeId == id && e.ServiceAreaId == serviceAreaId);

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

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}/service-areas/{serviceAreaId}")]
    public async Task<IActionResult> RemoveServiceArea(int id, int serviceAreaId)
    {
        var existing = await _context.EmployeeServiceAreas
            .FirstOrDefaultAsync(e => e.EmployeeId == id && e.ServiceAreaId == serviceAreaId);

        if (existing == null) return NotFound();

        _context.EmployeeServiceAreas.Remove(existing);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Equipment
    [HttpPost("{id}/equipment/{equipmentId}")]
    public async Task<IActionResult> AddEquipment(int id, int equipmentId, [FromQuery] bool isAvailable = true)
    {
        var employee = await _context.Employees.FindAsync(id);
        var equipment = await _context.Equipment.FindAsync(equipmentId);

        if (employee == null || equipment == null) return NotFound();

        var existing = await _context.EmployeeEquipment
            .FirstOrDefaultAsync(e => e.EmployeeId == id && e.EquipmentId == equipmentId);

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

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}/equipment/{equipmentId}")]
    public async Task<IActionResult> RemoveEquipment(int id, int equipmentId)
    {
        var existing = await _context.EmployeeEquipment
            .FirstOrDefaultAsync(e => e.EmployeeId == id && e.EquipmentId == equipmentId);

        if (existing == null) return NotFound();

        _context.EmployeeEquipment.Remove(existing);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Time Off
    [HttpGet("{id}/time-off")]
    public async Task<ActionResult<IEnumerable<EmployeeTimeOff>>> GetTimeOffs(int id, [FromQuery] DateTime? from = null)
    {
        var query = _context.EmployeeTimeOffs
            .Where(t => t.EmployeeId == id && !t.IsDeleted);

        if (from.HasValue)
            query = query.Where(t => t.EndDateTime >= from.Value);

        return await query.OrderBy(t => t.StartDateTime).ToListAsync();
    }

    [HttpPost("{id}/time-off")]
    public async Task<ActionResult<EmployeeTimeOff>> AddTimeOff(int id, CreateTimeOffRequest request)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null || employee.IsDeleted) return NotFound();

        var timeOff = new EmployeeTimeOff
        {
            EmployeeId = id,
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime,
            IsAllDay = request.IsAllDay,
            Type = request.Type,
            Reason = request.Reason,
            Status = TimeOffStatus.Approved // Auto-aprobado desde admin
        };

        _context.EmployeeTimeOffs.Add(timeOff);
        await _context.SaveChangesAsync();

        return Created($"/api/admin/employees/{id}/time-off/{timeOff.Id}", timeOff);
    }

    [HttpDelete("{id}/time-off/{timeOffId}")]
    public async Task<IActionResult> DeleteTimeOff(int id, int timeOffId)
    {
        var timeOff = await _context.EmployeeTimeOffs
            .FirstOrDefaultAsync(t => t.Id == timeOffId && t.EmployeeId == id);

        if (timeOff == null) return NotFound();

        timeOff.IsDeleted = true;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Schedules
    [HttpGet("{id}/schedules")]
    public async Task<ActionResult<IEnumerable<EmployeeSchedule>>> GetSchedules(int id)
    {
        return await _context.EmployeeSchedules
            .Where(s => s.EmployeeId == id && !s.IsDeleted)
            .OrderBy(s => s.DayOfWeek)
            .ToListAsync();
    }

    [HttpPost("{id}/schedules")]
    public async Task<ActionResult<EmployeeSchedule>> AddSchedule(int id, CreateScheduleAdminRequest request)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null || employee.IsDeleted) return NotFound();

        // Verificar si ya existe horario para ese día
        var existing = await _context.EmployeeSchedules
            .FirstOrDefaultAsync(s => s.EmployeeId == id && s.DayOfWeek == request.DayOfWeek && !s.IsDeleted);

        if (existing != null)
        {
            existing.StartTime = request.StartTime;
            existing.EndTime = request.EndTime;
            existing.BufferMinutes = request.BufferMinutes;
            existing.IsAvailable = request.IsAvailable;
        }
        else
        {
            _context.EmployeeSchedules.Add(new EmployeeSchedule
            {
                EmployeeId = id,
                DayOfWeek = request.DayOfWeek,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                BufferMinutes = request.BufferMinutes,
                IsAvailable = request.IsAvailable
            });
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{id}/schedules/{dayOfWeek}")]
    public async Task<IActionResult> DeleteSchedule(int id, DayOfWeek dayOfWeek)
    {
        var schedule = await _context.EmployeeSchedules
            .FirstOrDefaultAsync(s => s.EmployeeId == id && s.DayOfWeek == dayOfWeek);

        if (schedule == null) return NotFound();

        schedule.IsDeleted = true;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

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

public record CreateEmployeeAdminRequest(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string? Address,
    int? PrimaryServiceAreaId,
    int? MaxDailyHours = 8,
    int? MaxDailyServices = 4
);

public record UpdateEmployeeAdminRequest(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string? Address,
    int? PrimaryServiceAreaId,
    int? MaxDailyHours,
    int? MaxDailyServices,
    bool IsActive
);

public record CreateTimeOffRequest(
    DateTime StartDateTime,
    DateTime EndDateTime,
    bool IsAllDay,
    TimeOffType Type,
    string? Reason
);

public record CreateScheduleAdminRequest(
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    int BufferMinutes = 30,
    bool IsAvailable = true
);
