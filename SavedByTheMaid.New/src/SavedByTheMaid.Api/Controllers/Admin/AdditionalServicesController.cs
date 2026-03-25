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
public class AdditionalServicesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdditionalServicesController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdditionalServiceType>>> GetAll()
    {
        return await _context.AdditionalServiceTypes
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.Title)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AdditionalServiceType>> GetById(int id)
    {
        var service = await _context.AdditionalServiceTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);
        return service == null || service.IsDeleted ? NotFound() : service;
    }

    [HttpPost]
    public async Task<ActionResult<AdditionalServiceType>> Create(CreateAdditionalServiceRequest request)
    {
        var service = new AdditionalServiceType
        {
            Title = request.Title,
            Description = request.Description,
            Price = request.Price,
            AdditionalMinutes = request.AdditionalMinutes,
            IsActive = true
        };

        _context.AdditionalServiceTypes.Add(service);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = service.Id }, service);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateAdditionalServiceRequest request)
    {
        var service = await _context.AdditionalServiceTypes.FindAsync(id);
        if (service == null || service.IsDeleted) return NotFound();

        service.Title = request.Title;
        service.Description = request.Description;
        service.Price = request.Price;
        service.AdditionalMinutes = request.AdditionalMinutes;
        service.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var service = await _context.AdditionalServiceTypes.FindAsync(id);
        if (service == null) return NotFound();

        service.IsDeleted = true;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public record CreateAdditionalServiceRequest(
    string Title, 
    string? Description, 
    decimal Price, 
    int AdditionalMinutes = 30
);

public record UpdateAdditionalServiceRequest(
    string Title, 
    string? Description, 
    decimal Price, 
    int AdditionalMinutes,
    bool IsActive
);
