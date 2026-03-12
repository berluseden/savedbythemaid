using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Api.Auth;
using SavedByTheMaid.Infrastructure.Data;
using SavedByTheMaid.Domain.Entities;

namespace SavedByTheMaid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiceAreasController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ServiceAreasController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServiceArea>>> GetServiceAreas()
    {
        return await _context.ServiceAreas
            .Where(s => !s.IsDeleted)
            .Include(s => s.ZipCodes)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceArea>> GetServiceArea(int id)
    {
        var serviceArea = await _context.ServiceAreas
            .Include(s => s.ZipCodes)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (serviceArea == null)
        {
            return NotFound();
        }

        return serviceArea;
    }

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<ServiceArea>> CreateServiceArea(CreateServiceAreaRequest request)
    {
        var serviceArea = new ServiceArea
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true
        };

        _context.ServiceAreas.Add(serviceArea);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetServiceArea), new { id = serviceArea.Id }, serviceArea);
    }

    [HttpPost("{id}/zipcodes")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<ServiceAreaZip>> AddZipCode(int id, AddZipCodeRequest request)
    {
        var serviceArea = await _context.ServiceAreas.FindAsync(id);
        if (serviceArea == null || serviceArea.IsDeleted)
        {
            return NotFound();
        }

        // Check if it already exists
        var existing = await _context.ServiceAreaZips
            .FirstOrDefaultAsync(z => z.ZipCode == request.ZipCode);
        
        if (existing != null)
        {
            return Conflict($"Zip code {request.ZipCode} is already assigned to another area.");
        }

        var zip = new ServiceAreaZip
        {
            ZipCode = request.ZipCode,
            ServiceAreaId = id
        };

        _context.ServiceAreaZips.Add(zip);
        await _context.SaveChangesAsync();

        return Created($"/api/serviceareas/{id}/zipcodes/{zip.Id}", zip);
    }

    [HttpGet("by-zipcode/{zipCode}")]
    public async Task<ActionResult<ServiceArea>> GetByZipCode(string zipCode)
    {
        var serviceAreaZip = await _context.ServiceAreaZips
            .Include(z => z.ServiceArea)
            .FirstOrDefaultAsync(z => z.ZipCode == zipCode);

        if (serviceAreaZip?.ServiceArea == null)
        {
            return NotFound($"No service area configured for zip code {zipCode}");
        }

        return serviceAreaZip.ServiceArea;
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> UpdateServiceArea(int id, UpdateServiceAreaRequest request)
    {
        var serviceArea = await _context.ServiceAreas.FindAsync(id);
        if (serviceArea == null || serviceArea.IsDeleted)
        {
            return NotFound();
        }

        serviceArea.Name = request.Name;
        serviceArea.Description = request.Description;
        serviceArea.IsActive = request.IsActive;
        serviceArea.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> DeleteServiceArea(int id)
    {
        var serviceArea = await _context.ServiceAreas
            .Include(s => s.ZipCodes)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (serviceArea == null)
        {
            return NotFound();
        }

        // Soft delete
        serviceArea.IsDeleted = true;
        foreach (var zip in serviceArea.ZipCodes)
        {
            zip.IsDeleted = true;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}/zipcodes/{zipId}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> DeleteZipCode(int id, int zipId)
    {
        var zip = await _context.ServiceAreaZips
            .FirstOrDefaultAsync(z => z.Id == zipId && z.ServiceAreaId == id);

        if (zip == null)
        {
            return NotFound();
        }

        zip.IsDeleted = true;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public record CreateServiceAreaRequest(string Name, string? Description);
public record UpdateServiceAreaRequest(string Name, string? Description, bool IsActive);
public record AddZipCodeRequest(string ZipCode);
