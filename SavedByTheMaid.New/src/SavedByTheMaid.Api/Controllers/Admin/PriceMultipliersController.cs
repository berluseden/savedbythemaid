using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Api.Auth;
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

    public PriceMultipliersController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PriceMultiplier>>> GetAll()
    {
        return await _context.PriceMultipliers
            .Where(m => !m.IsDeleted)
            .Include(m => m.ServiceType)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PriceMultiplier>> GetById(int id)
    {
        var multiplier = await _context.PriceMultipliers
            .Include(m => m.ServiceType)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

        return multiplier == null ? NotFound() : multiplier;
    }

    [HttpPost]
    public async Task<ActionResult<PriceMultiplier>> Create(CreatePriceMultiplierRequest request)
    {
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
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = multiplier.Id }, multiplier);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdatePriceMultiplierRequest request)
    {
        var multiplier = await _context.PriceMultipliers.FindAsync(id);
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

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var multiplier = await _context.PriceMultipliers.FindAsync(id);
        if (multiplier == null) return NotFound();

        multiplier.IsDeleted = true;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Recurrence Discounts
    [HttpGet("recurrence-discounts")]
    public async Task<ActionResult<IEnumerable<RecurrenceDiscount>>> GetRecurrenceDiscounts()
    {
        return await _context.RecurrenceDiscounts
            .Where(d => !d.IsDeleted)
            .ToListAsync();
    }

    [HttpPost("recurrence-discounts")]
    public async Task<ActionResult<RecurrenceDiscount>> CreateRecurrenceDiscount(CreateRecurrenceDiscountRequest request)
    {
        // Check if one already exists for this recurrence type
        var existing = await _context.RecurrenceDiscounts
            .FirstOrDefaultAsync(d => d.RecurrenceType == request.RecurrenceType && !d.IsDeleted);

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

        await _context.SaveChangesAsync();
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
