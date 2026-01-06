using SavedByTheMaid.Domain.Common;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Equipamiento requerido para ciertos servicios (aspiradora industrial, escalera alta, etc.)
/// </summary>
public class Equipment : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navegación
    public virtual ICollection<ServiceTypeEquipment> ServiceTypes { get; set; } = new List<ServiceTypeEquipment>();
    public virtual ICollection<EmployeeEquipment> Employees { get; set; } = new List<EmployeeEquipment>();
}

/// <summary>
/// Equipamiento requerido por un tipo de servicio
/// </summary>
public class ServiceTypeEquipment : BaseEntity
{
    public int ServiceTypeId { get; set; }
    public virtual ServiceType? ServiceType { get; set; }

    public int EquipmentId { get; set; }
    public virtual Equipment? Equipment { get; set; }

    /// <summary>
    /// Si es obligatorio o recomendado
    /// </summary>
    public bool IsRequired { get; set; } = true;
}

/// <summary>
/// Equipamiento que posee o puede usar una empleada
/// </summary>
public class EmployeeEquipment : BaseEntity
{
    public int EmployeeId { get; set; }
    public virtual Employee? Employee { get; set; }

    public int EquipmentId { get; set; }
    public virtual Equipment? Equipment { get; set; }

    /// <summary>
    /// Si la empleada tiene este equipo disponible actualmente
    /// </summary>
    public bool IsAvailable { get; set; } = true;
}
