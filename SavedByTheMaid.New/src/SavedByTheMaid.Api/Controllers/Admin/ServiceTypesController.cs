using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Api.Auth;
using SavedByTheMaid.Infrastructure.Data;
using SavedByTheMaid.Domain.Entities;

namespace SavedByTheMaid.Api.Controllers;

[ApiController]
[Route("api/admin/service-types")]
[Authorize(Policy = Policies.AdminOnly)]
public class ServiceTypesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ServiceTypesController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServiceType>>> GetAll()
    {
        return await _context.ServiceTypes
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceType>> GetById(int id)
    {
        var serviceType = await _context.ServiceTypes
            .Include(s => s.RequiredEquipment)
                .ThenInclude(e => e.Equipment)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        return serviceType == null ? NotFound() : serviceType;
    }

    [HttpPost]
    public async Task<ActionResult<ServiceType>> Create(CreateServiceTypeRequest request)
    {
        var serviceType = new ServiceType
        {
            Name = request.Name,
            Description = request.Description,
            Cost = request.Cost,
            Price = request.Price,
            EstimatedMinutes = request.EstimatedMinutes,
            DisplayOrder = request.DisplayOrder,
            IsActive = true
        };

        _context.ServiceTypes.Add(serviceType);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = serviceType.Id }, serviceType);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateServiceTypeRequest request)
    {
        var serviceType = await _context.ServiceTypes.FindAsync(id);
        if (serviceType == null || serviceType.IsDeleted) return NotFound();

        serviceType.Name = request.Name;
        serviceType.Description = request.Description;
        serviceType.Cost = request.Cost;
        serviceType.Price = request.Price;
        serviceType.EstimatedMinutes = request.EstimatedMinutes;
        serviceType.DisplayOrder = request.DisplayOrder;
        serviceType.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var serviceType = await _context.ServiceTypes.FindAsync(id);
        if (serviceType == null) return NotFound();

        serviceType.IsDeleted = true;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Equipment requirements
    [HttpPost("{id}/equipment/{equipmentId}")]
    public async Task<IActionResult> AddRequiredEquipment(int id, int equipmentId, [FromQuery] bool isRequired = true)
    {
        var serviceType = await _context.ServiceTypes.FindAsync(id);
        var equipment = await _context.Equipment.FindAsync(equipmentId);

        if (serviceType == null || equipment == null) return NotFound();

        var existing = await _context.ServiceTypeEquipment
            .FirstOrDefaultAsync(e => e.ServiceTypeId == id && e.EquipmentId == equipmentId);

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

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}/equipment/{equipmentId}")]
    public async Task<IActionResult> RemoveRequiredEquipment(int id, int equipmentId)
    {
        var existing = await _context.ServiceTypeEquipment
            .FirstOrDefaultAsync(e => e.ServiceTypeId == id && e.EquipmentId == equipmentId);

        if (existing == null) return NotFound();

        _context.ServiceTypeEquipment.Remove(existing);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public record CreateServiceTypeRequest(
    string Name, 
    string? Description, 
    decimal Cost, 
    decimal Price, 
    int EstimatedMinutes = 60,
    int DisplayOrder = 0
);

public record UpdateServiceTypeRequest(
    string Name, 
    string? Description, 
    decimal Cost, 
    decimal Price, 
    int EstimatedMinutes,
    int DisplayOrder,
    bool IsActive
);
