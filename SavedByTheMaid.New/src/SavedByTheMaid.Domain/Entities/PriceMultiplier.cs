using SavedByTheMaid.Domain.Common;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Multiplicadores de precio según condiciones (m², suciedad, mascotas, etc.)
/// </summary>
public class PriceMultiplier : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }

    /// <summary>
    /// Tipo de condición que activa este multiplicador
    /// </summary>
    public MultiplierConditionType ConditionType { get; set; }

    /// <summary>
    /// Factor multiplicador (ej: 1.2 = +20%, 0.9 = -10%)
    /// </summary>
    public decimal Factor { get; set; } = 1.0m;

    /// <summary>
    /// Valor mínimo del rango (ej: para m² de 100 a 200, MinValue=100)
    /// </summary>
    public decimal? MinValue { get; set; }

    /// <summary>
    /// Valor máximo del rango
    /// </summary>
    public decimal? MaxValue { get; set; }

    /// <summary>
    /// Si aplica a tiempo además de precio
    /// </summary>
    public bool AppliesToTime { get; set; } = true;

    /// <summary>
    /// Si aplica a precio
    /// </summary>
    public bool AppliesToPrice { get; set; } = true;

    /// <summary>
    /// Tipo de servicio específico (null = todos)
    /// </summary>
    public int? ServiceTypeId { get; set; }
    public virtual ServiceType? ServiceType { get; set; }

    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
}

/// <summary>
/// Descuentos por recurrencia
/// </summary>
public class RecurrenceDiscount : BaseEntity
{
    public RecurrenceType RecurrenceType { get; set; }

    /// <summary>
    /// Porcentaje de descuento (ej: 0.10 = 10% descuento)
    /// </summary>
    public decimal DiscountPercent { get; set; }

    public bool IsActive { get; set; } = true;
}
