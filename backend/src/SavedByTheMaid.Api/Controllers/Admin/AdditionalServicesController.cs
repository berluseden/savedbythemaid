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
public class AdditionalServicesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<CreateAdditionalServiceRequest> _createValidator;
    private readonly IValidator<UpdateAdditionalServiceRequest> _updateValidator;

    public AdditionalServicesController(
        ApplicationDbContext context,
        IValidator<CreateAdditionalServiceRequest> createValidator,
        IValidator<UpdateAdditionalServiceRequest> updateValidator)
    {
        _context = context;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AdditionalServiceTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<AdditionalServiceTypeDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        return await _context.AdditionalServiceTypes
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.Title)
            .Select(s => new AdditionalServiceTypeDto
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                Price = s.Price,
                AdditionalMinutes = s.AdditionalMinutes,
                IsActive = s.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AdditionalServiceTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdditionalServiceTypeDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var service = await _context.AdditionalServiceTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (service == null || service.IsDeleted) return NotFound();

        return new AdditionalServiceTypeDto
        {
            Id = service.Id,
            Title = service.Title,
            Description = service.Description,
            Price = service.Price,
            AdditionalMinutes = service.AdditionalMinutes,
            IsActive = service.IsActive
        };
    }

    [HttpPost]
    [ProducesResponseType(typeof(AdditionalServiceTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdditionalServiceTypeDto>> Create(CreateAdditionalServiceRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = await _createValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var service = new AdditionalServiceType
        {
            Title = request.Title,
            Description = request.Description,
            Price = request.Price,
            AdditionalMinutes = request.AdditionalMinutes,
            IsActive = true
        };

        _context.AdditionalServiceTypes.Add(service);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new AdditionalServiceTypeDto
        {
            Id = service.Id,
            Title = service.Title,
            Description = service.Description,
            Price = service.Price,
            AdditionalMinutes = service.AdditionalMinutes,
            IsActive = service.IsActive
        };

        return CreatedAtAction(nameof(GetById), new { id = service.Id }, dto);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateAdditionalServiceRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = await _updateValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var service = await _context.AdditionalServiceTypes.FindAsync(new object[] { id }, cancellationToken);
        if (service == null || service.IsDeleted) return NotFound();

        service.Title = request.Title;
        service.Description = request.Description;
        service.Price = request.Price;
        service.AdditionalMinutes = request.AdditionalMinutes;
        service.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var service = await _context.AdditionalServiceTypes.FindAsync(new object[] { id }, cancellationToken);
        if (service == null || service.IsDeleted)
            return NoContent(); // Idempotent

        service.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public record AdditionalServiceTypeDto
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public int AdditionalMinutes { get; init; }
    public bool IsActive { get; init; }
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
