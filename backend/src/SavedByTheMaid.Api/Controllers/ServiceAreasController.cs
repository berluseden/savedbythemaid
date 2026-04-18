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
[Route("api/[controller]")]
[Authorize(Policy = Policies.AdminOnly)]
public class ServiceAreasController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<CreateServiceAreaRequest> _createValidator;
    private readonly IValidator<UpdateServiceAreaRequest> _updateValidator;
    private readonly IValidator<AddZipCodeRequest> _zipValidator;

    public ServiceAreasController(
        ApplicationDbContext context,
        IValidator<CreateServiceAreaRequest> createValidator,
        IValidator<UpdateServiceAreaRequest> updateValidator,
        IValidator<AddZipCodeRequest> zipValidator)
    {
        _context = context;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _zipValidator = zipValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ServiceAreaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ServiceAreaDto>>> GetServiceAreas(CancellationToken cancellationToken = default)
    {
        return await _context.ServiceAreas
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .Include(s => s.ZipCodes)
            .Select(s => new ServiceAreaDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                IsActive = s.IsActive,
                ZipCodes = s.ZipCodes.Where(z => !z.IsDeleted).Select(z => z.ZipCode).ToList()
            })
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ServiceAreaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceAreaDto>> GetServiceArea(int id, CancellationToken cancellationToken = default)
    {
        var serviceArea = await _context.ServiceAreas
            .AsNoTracking()
            .Include(s => s.ZipCodes)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        if (serviceArea == null) return NotFound();

        return new ServiceAreaDto
        {
            Id = serviceArea.Id,
            Name = serviceArea.Name,
            Description = serviceArea.Description,
            IsActive = serviceArea.IsActive,
            ZipCodes = serviceArea.ZipCodes.Where(z => !z.IsDeleted).Select(z => z.ZipCode).ToList()
        };
    }

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(typeof(ServiceAreaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceAreaDto>> CreateServiceArea(CreateServiceAreaRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = await _createValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var serviceArea = new ServiceArea
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true
        };

        _context.ServiceAreas.Add(serviceArea);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new ServiceAreaDto
        {
            Id = serviceArea.Id,
            Name = serviceArea.Name,
            Description = serviceArea.Description,
            IsActive = serviceArea.IsActive
        };

        return CreatedAtAction(nameof(GetServiceArea), new { id = serviceArea.Id }, dto);
    }

    [HttpPost("{id}/zipcodes")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServiceAreaZipDto>> AddZipCode(int id, AddZipCodeRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = await _zipValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var serviceArea = await _context.ServiceAreas.FindAsync(new object[] { id }, cancellationToken);
        if (serviceArea == null || serviceArea.IsDeleted)
            return NotFound();

        var existing = await _context.ServiceAreaZips
            .FirstOrDefaultAsync(z => z.ZipCode == request.ZipCode, cancellationToken);

        if (existing != null)
            return Conflict($"Zip code {request.ZipCode} is already assigned to another area.");

        var zip = new ServiceAreaZip
        {
            ZipCode = request.ZipCode,
            ServiceAreaId = id
        };

        _context.ServiceAreaZips.Add(zip);
        await _context.SaveChangesAsync(cancellationToken);

        var zipDto = new ServiceAreaZipDto { Id = zip.Id, ZipCode = zip.ZipCode, ServiceAreaId = id };
        return Created($"/api/service-areas/{id}/zipcodes/{zip.Id}", zipDto);
    }

    [HttpGet("by-zipcode/{zipCode}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ServiceAreaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceAreaDto>> GetByZipCode(string zipCode, CancellationToken cancellationToken = default)
    {
        var serviceAreaZip = await _context.ServiceAreaZips
            .AsNoTracking()
            .Include(z => z.ServiceArea)
            .FirstOrDefaultAsync(z => z.ZipCode == zipCode, cancellationToken);

        if (serviceAreaZip?.ServiceArea == null)
            return NotFound($"No service area configured for zip code {zipCode}");

        var sa = serviceAreaZip.ServiceArea;
        return new ServiceAreaDto
        {
            Id = sa.Id,
            Name = sa.Name,
            Description = sa.Description,
            IsActive = sa.IsActive
        };
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateServiceArea(int id, UpdateServiceAreaRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = await _updateValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var serviceArea = await _context.ServiceAreas.FindAsync(new object[] { id }, cancellationToken);
        if (serviceArea == null || serviceArea.IsDeleted)
            return NotFound();

        serviceArea.Name = request.Name;
        serviceArea.Description = request.Description;
        serviceArea.IsActive = request.IsActive;
        serviceArea.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteServiceArea(int id, CancellationToken cancellationToken = default)
    {
        var serviceArea = await _context.ServiceAreas
            .Include(s => s.ZipCodes)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (serviceArea == null || serviceArea.IsDeleted)
            return NoContent(); // Idempotent

        serviceArea.IsDeleted = true;
        foreach (var zip in serviceArea.ZipCodes)
            zip.IsDeleted = true;

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}/zipcodes/{zipId}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteZipCode(int id, int zipId, CancellationToken cancellationToken = default)
    {
        var zip = await _context.ServiceAreaZips
            .FirstOrDefaultAsync(z => z.Id == zipId && z.ServiceAreaId == id, cancellationToken);

        if (zip == null)
            return NotFound();

        zip.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public record CreateServiceAreaRequest(string Name, string? Description);
public record UpdateServiceAreaRequest(string Name, string? Description, bool IsActive);
public record AddZipCodeRequest(string ZipCode);

public record ServiceAreaDto
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public List<string> ZipCodes { get; init; } = new();
}

public record ServiceAreaZipDto
{
    public int Id { get; init; }
    public string ZipCode { get; init; } = "";
    public int ServiceAreaId { get; init; }
}
