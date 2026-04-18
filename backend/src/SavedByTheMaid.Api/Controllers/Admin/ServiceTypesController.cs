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
[Route("api/admin/service-types")]
[Authorize(Policy = Policies.AdminOnly)]
public class ServiceTypesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<CreateServiceTypeRequest> _createValidator;
    private readonly IValidator<UpdateServiceTypeRequest> _updateValidator;

    public ServiceTypesController(
        ApplicationDbContext context,
        IValidator<CreateServiceTypeRequest> createValidator,
        IValidator<UpdateServiceTypeRequest> updateValidator)
    {
        _context = context;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ServiceTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ServiceTypeDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        return await _context.ServiceTypes
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new ServiceTypeDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Price = s.Price,
                PricePerBedroom = s.PricePerBedroom,
                PricePerBathroom = s.PricePerBathroom,
                EstimatedMinutes = s.EstimatedMinutes,
                MinutesPerBedroom = s.MinutesPerBedroom,
                MinutesPerBathroom = s.MinutesPerBathroom,
                DisplayOrder = s.DisplayOrder,
                IsActive = s.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ServiceTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceTypeDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var serviceType = await _context.ServiceTypes
            .AsNoTracking()
            .Include(s => s.RequiredEquipment)
                .ThenInclude(e => e.Equipment)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        if (serviceType == null) return NotFound();

        return new ServiceTypeDto
        {
            Id = serviceType.Id,
            Name = serviceType.Name,
            Description = serviceType.Description,
            Price = serviceType.Price,
            PricePerBedroom = serviceType.PricePerBedroom,
            PricePerBathroom = serviceType.PricePerBathroom,
            EstimatedMinutes = serviceType.EstimatedMinutes,
            MinutesPerBedroom = serviceType.MinutesPerBedroom,
            MinutesPerBathroom = serviceType.MinutesPerBathroom,
            DisplayOrder = serviceType.DisplayOrder,
            IsActive = serviceType.IsActive,
            RequiredEquipment = serviceType.RequiredEquipment.Select(e => new ServiceTypeEquipmentDto
            {
                EquipmentId = e.EquipmentId,
                EquipmentName = e.Equipment?.Name ?? "",
                IsRequired = e.IsRequired
            }).ToList()
        };
    }

    [HttpPost]
    [ProducesResponseType(typeof(ServiceTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceTypeDto>> Create(CreateServiceTypeRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = await _createValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var serviceType = new ServiceType
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            PricePerBedroom = request.PricePerBedroom,
            PricePerBathroom = request.PricePerBathroom,
            EstimatedMinutes = request.EstimatedMinutes,
            MinutesPerBedroom = request.MinutesPerBedroom,
            MinutesPerBathroom = request.MinutesPerBathroom,
            DisplayOrder = request.DisplayOrder,
            IsActive = true
        };

        _context.ServiceTypes.Add(serviceType);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new ServiceTypeDto
        {
            Id = serviceType.Id,
            Name = serviceType.Name,
            Description = serviceType.Description,
            Price = serviceType.Price,
            PricePerBedroom = serviceType.PricePerBedroom,
            PricePerBathroom = serviceType.PricePerBathroom,
            EstimatedMinutes = serviceType.EstimatedMinutes,
            MinutesPerBedroom = serviceType.MinutesPerBedroom,
            MinutesPerBathroom = serviceType.MinutesPerBathroom,
            DisplayOrder = serviceType.DisplayOrder,
            IsActive = serviceType.IsActive
        };

        return CreatedAtAction(nameof(GetById), new { id = serviceType.Id }, dto);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateServiceTypeRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = await _updateValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var serviceType = await _context.ServiceTypes.FindAsync(new object[] { id }, cancellationToken);
        if (serviceType == null || serviceType.IsDeleted) return NotFound();

        serviceType.Name = request.Name;
        serviceType.Description = request.Description;
        serviceType.Price = request.Price;
        serviceType.PricePerBedroom = request.PricePerBedroom;
        serviceType.PricePerBathroom = request.PricePerBathroom;
        serviceType.EstimatedMinutes = request.EstimatedMinutes;
        serviceType.MinutesPerBedroom = request.MinutesPerBedroom;
        serviceType.MinutesPerBathroom = request.MinutesPerBathroom;
        serviceType.DisplayOrder = request.DisplayOrder;
        serviceType.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var serviceType = await _context.ServiceTypes.FindAsync(new object[] { id }, cancellationToken);
        if (serviceType == null || serviceType.IsDeleted)
            return NoContent(); // Idempotent

        serviceType.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/equipment/{equipmentId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddRequiredEquipment(int id, int equipmentId, [FromQuery] bool isRequired = true, CancellationToken cancellationToken = default)
    {
        var serviceType = await _context.ServiceTypes.FindAsync(new object[] { id }, cancellationToken);
        var equipment = await _context.Equipment.FindAsync(new object[] { equipmentId }, cancellationToken);

        if (serviceType == null || equipment == null) return NotFound();

        var existing = await _context.ServiceTypeEquipment
            .FirstOrDefaultAsync(e => e.ServiceTypeId == id && e.EquipmentId == equipmentId, cancellationToken);

        if (existing != null)
        {
            existing.IsRequired = isRequired;
        }
        else
        {
            _context.ServiceTypeEquipment.Add(new ServiceTypeEquipment
            {
                ServiceTypeId = id,
                EquipmentId = equipmentId,
                IsRequired = isRequired
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}/equipment/{equipmentId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRequiredEquipment(int id, int equipmentId, CancellationToken cancellationToken = default)
    {
        var existing = await _context.ServiceTypeEquipment
            .FirstOrDefaultAsync(e => e.ServiceTypeId == id && e.EquipmentId == equipmentId, cancellationToken);

        if (existing == null) return NotFound();

        _context.ServiceTypeEquipment.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
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
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
    public List<ServiceTypeEquipmentDto> RequiredEquipment { get; init; } = new();
}

public record ServiceTypeEquipmentDto
{
    public int EquipmentId { get; init; }
    public string EquipmentName { get; init; } = "";
    public bool IsRequired { get; init; }
}

public record CreateServiceTypeRequest(
    string Name,
    string? Description,
    decimal Price,
    decimal PricePerBedroom = 15.00m,
    decimal PricePerBathroom = 20.00m,
    int EstimatedMinutes = 60,
    int MinutesPerBedroom = 20,
    int MinutesPerBathroom = 15,
    int DisplayOrder = 0
);

public record UpdateServiceTypeRequest(
    string Name,
    string? Description,
    decimal Price,
    decimal PricePerBedroom,
    decimal PricePerBathroom,
    int EstimatedMinutes,
    int MinutesPerBedroom,
    int MinutesPerBathroom,
    int DisplayOrder,
    bool IsActive
);
