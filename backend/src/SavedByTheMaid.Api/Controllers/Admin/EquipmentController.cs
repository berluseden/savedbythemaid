using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Api.Auth;
using SavedByTheMaid.Api.Extensions;
using SavedByTheMaid.Infrastructure.Data;
using SavedByTheMaid.Domain.Entities;

namespace SavedByTheMaid.Api.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Policy = Policies.AdminOnly)]
public class EquipmentController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<CreateEquipmentRequest> _createValidator;
    private readonly IValidator<UpdateEquipmentRequest> _updateValidator;

    public EquipmentController(
        ApplicationDbContext context,
        IValidator<CreateEquipmentRequest> createValidator,
        IValidator<UpdateEquipmentRequest> updateValidator)
    {
        _context = context;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EquipmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<EquipmentDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        return await _context.Equipment
            .AsNoTracking()
            .Where(e => !e.IsDeleted)
            .OrderBy(e => e.Name)
            .Select(e => new EquipmentDto { Id = e.Id, Name = e.Name, Description = e.Description, IsActive = e.IsActive })
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EquipmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EquipmentDetailDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var equipment = await _context.Equipment
            .AsNoTracking()
            .Include(e => e.ServiceTypes)
                .ThenInclude(st => st.ServiceType)
            .Include(e => e.Employees)
                .ThenInclude(ee => ee.Employee)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);

        if (equipment == null) return NotFound();

        return new EquipmentDetailDto
        {
            Id = equipment.Id,
            Name = equipment.Name,
            Description = equipment.Description,
            IsActive = equipment.IsActive,
            ServiceTypes = equipment.ServiceTypes.Select(st => new EquipmentServiceTypeDto
            {
                ServiceTypeId = st.ServiceTypeId,
                ServiceTypeName = st.ServiceType?.Name ?? "",
                IsRequired = st.IsRequired
            }).ToList(),
            Employees = equipment.Employees.Select(ee => new EquipmentEmployeeDto
            {
                EmployeeId = ee.EmployeeId,
                EmployeeName = ee.Employee != null ? $"{ee.Employee.FirstName} {ee.Employee.LastName}" : "",
                IsAvailable = ee.IsAvailable
            }).ToList()
        };
    }

    [HttpPost]
    [ProducesResponseType(typeof(EquipmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EquipmentDto>> Create(CreateEquipmentRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = await _createValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var equipment = new Equipment
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true
        };

        _context.Equipment.Add(equipment);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new EquipmentDto { Id = equipment.Id, Name = equipment.Name, Description = equipment.Description, IsActive = equipment.IsActive };
        return CreatedAtAction(nameof(GetById), new { id = equipment.Id }, dto);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateEquipmentRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = await _updateValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var equipment = await _context.Equipment.FindAsync(new object[] { id }, cancellationToken);
        if (equipment == null || equipment.IsDeleted) return NotFound();

        equipment.Name = request.Name;
        equipment.Description = request.Description;
        equipment.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var equipment = await _context.Equipment.FindAsync(new object[] { id }, cancellationToken);
        if (equipment == null || equipment.IsDeleted)
            return NoContent(); // Idempotent

        equipment.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public record CreateEquipmentRequest(string Name, string? Description);
public record UpdateEquipmentRequest(string Name, string? Description, bool IsActive);

public record EquipmentDto
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public bool IsActive { get; init; }
}

public record EquipmentDetailDto : EquipmentDto
{
    public List<EquipmentServiceTypeDto> ServiceTypes { get; init; } = new();
    public List<EquipmentEmployeeDto> Employees { get; init; } = new();
}

public record EquipmentServiceTypeDto
{
    public int ServiceTypeId { get; init; }
    public string ServiceTypeName { get; init; } = "";
    public bool IsRequired { get; init; }
}

public record EquipmentEmployeeDto
{
    public int EmployeeId { get; init; }
    public string EmployeeName { get; init; } = "";
    public bool IsAvailable { get; init; }
}
