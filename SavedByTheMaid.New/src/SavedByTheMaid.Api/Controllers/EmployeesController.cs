using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Api.Auth;
using SavedByTheMaid.Infrastructure.Data;
using SavedByTheMaid.Domain.Entities;

namespace SavedByTheMaid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EmployeesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeePublicDto>>> GetEmployees()
    {
        var employees = await _context.Employees
            .AsNoTracking()
            .Where(e => !e.IsDeleted && e.IsActive)
            .Include(e => e.PrimaryServiceArea)
            .Select(e => new EmployeePublicDto
            {
                Id = e.Id,
                FirstName = e.FirstName,
                LastName = e.LastName,
                PrimaryServiceAreaId = e.PrimaryServiceAreaId,
                PrimaryServiceAreaName = e.PrimaryServiceArea != null ? e.PrimaryServiceArea.Name : null
            })
            .ToListAsync();

        return Ok(employees);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EmployeePublicDetailDto>> GetEmployee(int id)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .Include(e => e.PrimaryServiceArea)
            .Include(e => e.Schedules)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        if (employee == null)
        {
            return NotFound();
        }

        return Ok(new EmployeePublicDetailDto
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            PrimaryServiceAreaId = employee.PrimaryServiceAreaId,
            PrimaryServiceAreaName = employee.PrimaryServiceArea?.Name,
            Schedules = employee.Schedules.Select(s => new EmployeeScheduleDto
            {
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                IsAvailable = s.IsAvailable
            }).ToList()
        });
    }

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<Employee>> CreateEmployee(CreateEmployeeRequest request)
    {
        var employee = new Employee
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            PrimaryServiceAreaId = request.PrimaryServiceAreaId,
            IsActive = true
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
    }

    [HttpGet("{id}/availability")]
    public async Task<ActionResult<IEnumerable<EmployeeSchedule>>> GetAvailability(int id)
    {
        var schedules = await _context.EmployeeSchedules
            .AsNoTracking()
            .Where(s => s.EmployeeId == id && s.IsAvailable)
            .OrderBy(s => s.DayOfWeek)
            .ToListAsync();

        return Ok(schedules);
    }

    [HttpPost("{id}/schedule")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<EmployeeSchedule>> AddSchedule(int id, CreateScheduleRequest request)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null || employee.IsDeleted)
        {
            return NotFound();
        }

        if (request.EndTime <= request.StartTime)
        {
            return BadRequest(new { message = "EndTime must be greater than StartTime" });
        }

        var schedule = new EmployeeSchedule
        {
            EmployeeId = id,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            BufferMinutes = request.BufferMinutes,
            IsAvailable = true
        };

        _context.EmployeeSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        return Created($"/api/employees/{id}/schedule/{schedule.Id}", schedule);
    }
}

/// <summary>
/// Public DTO - no PII (email, phone) exposed
/// </summary>
public record EmployeePublicDto
{
    public int Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public int? PrimaryServiceAreaId { get; init; }
    public string? PrimaryServiceAreaName { get; init; }
}

/// <summary>
/// Public detail DTO - includes schedule but no PII
/// </summary>
public record EmployeePublicDetailDto
{
    public int Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public int? PrimaryServiceAreaId { get; init; }
    public string? PrimaryServiceAreaName { get; init; }
    public List<EmployeeScheduleDto> Schedules { get; init; } = new();
}

public record EmployeeScheduleDto
{
    public DayOfWeek DayOfWeek { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public bool IsAvailable { get; init; }
}

public record CreateEmployeeRequest(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string? Address,
    int? PrimaryServiceAreaId
);

public record CreateScheduleRequest(
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    int BufferMinutes = 30
);
