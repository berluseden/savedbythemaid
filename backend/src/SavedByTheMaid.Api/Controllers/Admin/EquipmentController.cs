using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Api.Auth;
using SavedByTheMaid.Infrastructure.Data;
using SavedByTheMaid.Domain.Entities;

namespace SavedByTheMaid.Api.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Policy = Policies.AdminOnly)]
public class EquipmentController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EquipmentController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Equipment>>> GetAll()
    {
        return await _context.Equipment
            .AsNoTracking()
            .Where(e => !e.IsDeleted)
            .OrderBy(e => e.Name)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Equipment>> GetById(int id)
    {
        var equipment = await _context.Equipment
            .AsNoTracking()
            .Include(e => e.ServiceTypes)
                .ThenInclude(st => st.ServiceType)
            .Include(e => e.Employees)
                .ThenInclude(ee => ee.Employee)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        return equipment == null ? NotFound() : equipment;
    }

    [HttpPost]
    public async Task<ActionResult<Equipment>> Create(CreateEquipmentRequest request)
    {
        var equipment = new Equipment
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true
        };

        _context.Equipment.Add(equipment);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = equipment.Id }, equipment);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateEquipmentRequest request)
    {
        var equipment = await _context.Equipment.FindAsync(id);
        if (equipment == null || equipment.IsDeleted) return NotFound();

        equipment.Name = request.Name;
        equipment.Description = request.Description;
        equipment.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var equipment = await _context.Equipment.FindAsync(id);
        if (equipment == null) return NotFound();

        equipment.IsDeleted = true;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public record CreateEquipmentRequest(string Name, string? Description);
public record UpdateEquipmentRequest(string Name, string? Description, bool IsActive);
