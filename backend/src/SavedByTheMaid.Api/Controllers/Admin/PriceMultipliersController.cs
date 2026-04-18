using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Api.Auth;
using SavedByTheMaid.Api.Extensions;
using SavedByTheMaid.Infrastructure.Data;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Api.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Policy = Policies.AdminOnly)]
public class PriceMultipliersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<CreatePriceMultiplierRequest> _createValidator;
    private readonly IValidator<UpdatePriceMultiplierRequest> _updateValidator;
    private readonly IValidator<CreateRecurrenceDiscountRequest> _discountValidator;

    public PriceMultipliersController(
        ApplicationDbContext context,
        IValidator<CreatePriceMultiplierRequest> createValidator,
        IValidator<UpdatePriceMultiplierRequest> updateValidator,
        IValidator<CreateRecurrenceDiscountRequest> discountValidator)
    {
        _context = context;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _discountValidator = discountValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PriceMultiplierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<PriceMultiplierDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        return await _context.PriceMultipliers
            .AsNoTracking()
            .Where(m => !m.IsDeleted)
            .Include(m => m.ServiceType)
            .OrderBy(m => m.DisplayOrder)
            .Select(m => new PriceMultiplierDto
            {
                Id = m.Id,
                Name = m.Name,
                Description = m.Description,
                ConditionType = m.ConditionType,
                Factor = m.Factor,
                MinValue = m.MinValue,
                MaxValue = m.MaxValue,
                AppliesToTime = m.AppliesToTime,
                AppliesToPrice = m.AppliesToPrice,
                ServiceTypeId = m.ServiceTypeId,
                ServiceTypeName = m.ServiceType != null ? m.ServiceType.Name : null,
                DisplayOrder = m.DisplayOrder,
                IsActive = m.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PriceMultiplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceMultiplierDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var multiplier = await _context.PriceMultipliers
            .AsNoTracking()
            .Include(m => m.ServiceType)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken);

        if (multiplier == null) return NotFound();

        return new PriceMultiplierDto
        {
            Id = multiplier.Id,
            Name = multiplier.Name,
            Description = multiplier.Description,
            ConditionType = multiplier.ConditionType,
            Factor = multiplier.Factor,
            MinValue = multiplier.MinValue,
            MaxValue = multiplier.MaxValue,
            AppliesToTime = multiplier.AppliesToTime,
            AppliesToPrice = multiplier.AppliesToPrice,
            ServiceTypeId = multiplier.ServiceTypeId,
            ServiceTypeName = multiplier.ServiceType?.Name,
            DisplayOrder = multiplier.DisplayOrder,
            IsActive = multiplier.IsActive
        };
    }

    [HttpPost]
    [ProducesResponseType(typeof(PriceMultiplierDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PriceMultiplierDto>> Create(
        CreatePriceMultiplierRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = await _createValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var multiplier = new PriceMultiplier
        {
            Name = request.Name,
            Description = request.Description,
            ConditionType = request.ConditionType,
            Factor = request.Factor,
            MinValue = request.MinValue,
            MaxValue = request.MaxValue,
            AppliesToTime = request.AppliesToTime,
            AppliesToPrice = request.AppliesToPrice,
            ServiceTypeId = request.ServiceTypeId,
            DisplayOrder = request.DisplayOrder,
            IsActive = true
        };

        _context.PriceMultipliers.Add(multiplier);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new PriceMultiplierDto
        {
            Id = multiplier.Id,
            Name = multiplier.Name,
            Description = multiplier.Description,
            ConditionType = multiplier.ConditionType,
            Factor = multiplier.Factor,
            MinValue = multiplier.MinValue,
            MaxValue = multiplier.MaxValue,
            AppliesToTime = multiplier.AppliesToTime,
            AppliesToPrice = multiplier.AppliesToPrice,
            ServiceTypeId = multiplier.ServiceTypeId,
            DisplayOrder = multiplier.DisplayOrder,
            IsActive = multiplier.IsActive
        };

        return CreatedAtAction(nameof(GetById), new { id = multiplier.Id }, dto);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        UpdatePriceMultiplierRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = await _updateValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var multiplier = await _context.PriceMultipliers.FindAsync(new object[] { id }, cancellationToken);
        if (multiplier == null || multiplier.IsDeleted) return NotFound();

        multiplier.Name = request.Name;
        multiplier.Description = request.Description;
        multiplier.ConditionType = request.ConditionType;
        multiplier.Factor = request.Factor;
        multiplier.MinValue = request.MinValue;
        multiplier.MaxValue = request.MaxValue;
        multiplier.AppliesToTime = request.AppliesToTime;
        multiplier.AppliesToPrice = request.AppliesToPrice;
        multiplier.ServiceTypeId = request.ServiceTypeId;
        multiplier.DisplayOrder = request.DisplayOrder;
        multiplier.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var multiplier = await _context.PriceMultipliers.FindAsync(new object[] { id }, cancellationToken);
        if (multiplier == null || multiplier.IsDeleted)
            return NoContent(); // Idempotent

        multiplier.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // Recurrence Discounts
    [HttpGet("recurrence-discounts")]
    [ProducesResponseType(typeof(IEnumerable<RecurrenceDiscount>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RecurrenceDiscount>>> GetRecurrenceDiscounts(CancellationToken cancellationToken = default)
    {
        return await _context.RecurrenceDiscounts
            .AsNoTracking()
            .Where(d => !d.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    [HttpPost("recurrence-discounts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecurrenceDiscount>> CreateRecurrenceDiscount(
        CreateRecurrenceDiscountRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = await _discountValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var existing = await _context.RecurrenceDiscounts
            .FirstOrDefaultAsync(d => d.RecurrenceType == request.RecurrenceType && !d.IsDeleted, cancellationToken);

        if (existing != null)
        {
            existing.DiscountPercent = request.DiscountPercent;
            existing.IsActive = true;
        }
        else
        {
            _context.RecurrenceDiscounts.Add(new RecurrenceDiscount
            {
                RecurrenceType = request.RecurrenceType,
                DiscountPercent = request.DiscountPercent,
                IsActive = true
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok();
    }
}

public record CreatePriceMultiplierRequest(
    string Name,
    string? Description,
    MultiplierConditionType ConditionType,
    decimal Factor,
    decimal? MinValue,
    decimal? MaxValue,
    bool AppliesToTime = true,
    bool AppliesToPrice = true,
    int? ServiceTypeId = null,
    int DisplayOrder = 0
);

public record UpdatePriceMultiplierRequest(
    string Name,
    string? Description,
    MultiplierConditionType ConditionType,
    decimal Factor,
    decimal? MinValue,
    decimal? MaxValue,
    bool AppliesToTime,
    bool AppliesToPrice,
    int? ServiceTypeId,
    int DisplayOrder,
    bool IsActive
);

public record CreateRecurrenceDiscountRequest(
    RecurrenceType RecurrenceType,
    decimal DiscountPercent
);

public record PriceMultiplierDto
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public MultiplierConditionType ConditionType { get; init; }
    public decimal Factor { get; init; }
    public decimal? MinValue { get; init; }
    public decimal? MaxValue { get; init; }
    public bool AppliesToTime { get; init; }
    public bool AppliesToPrice { get; init; }
    public int? ServiceTypeId { get; init; }
    public string? ServiceTypeName { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
}
