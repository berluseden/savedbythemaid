using SavedByTheMaid.Domain.Common;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Price multipliers based on conditions (sq ft, dirt level, pets, etc.)
/// </summary>
public class PriceMultiplier : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }

    /// <summary>
    /// Type of condition that triggers this multiplier
    /// </summary>
    public MultiplierConditionType ConditionType { get; set; }

    /// <summary>
    /// Multiplier factor (e.g., 1.2 = +20%, 0.9 = -10%)
    /// </summary>
    public decimal Factor { get; set; } = 1.0m;

    /// <summary>
    /// Minimum range value (e.g., for sq ft from 100 to 200, MinValue=100)
    /// </summary>
    public decimal? MinValue { get; set; }

    /// <summary>
    /// Maximum range value
    /// </summary>
    public decimal? MaxValue { get; set; }

    /// <summary>
    /// Whether it applies to time in addition to price
    /// </summary>
    public bool AppliesToTime { get; set; } = true;

    /// <summary>
    /// Whether it applies to price
    /// </summary>
    public bool AppliesToPrice { get; set; } = true;

    /// <summary>
    /// Specific service type (null = all)
    /// </summary>
    public int? ServiceTypeId { get; set; }
    public virtual ServiceType? ServiceType { get; set; }

    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
}

/// <summary>
/// Recurrence discounts
/// </summary>
public class RecurrenceDiscount : BaseEntity
{
    public RecurrenceType RecurrenceType { get; set; }

    /// <summary>
    /// Discount percentage (e.g., 0.10 = 10% discount)
    /// </summary>
    public decimal DiscountPercent { get; set; }

    public bool IsActive { get; set; } = true;
}
