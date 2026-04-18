using SavedByTheMaid.Domain.Common;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Equipment required for certain services (industrial vacuum, tall ladder, etc.)
/// </summary>
public class Equipment : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation
    public virtual ICollection<ServiceTypeEquipment> ServiceTypes { get; set; } = new List<ServiceTypeEquipment>();
    public virtual ICollection<EmployeeEquipment> Employees { get; set; } = new List<EmployeeEquipment>();
}

/// <summary>
/// Equipment required by a service type
/// </summary>
public class ServiceTypeEquipment : BaseEntity
{
    public int ServiceTypeId { get; set; }
    public virtual ServiceType? ServiceType { get; set; }

    public int EquipmentId { get; set; }
    public virtual Equipment? Equipment { get; set; }

    /// <summary>
    /// Whether it is required or recommended
    /// </summary>
    public bool IsRequired { get; set; } = true;
}

/// <summary>
/// Equipment owned or usable by an employee
/// </summary>
public class EmployeeEquipment : BaseEntity
{
    public int EmployeeId { get; set; }
    public virtual Employee? Employee { get; set; }

    public int EquipmentId { get; set; }
    public virtual Equipment? Equipment { get; set; }

    /// <summary>
    /// Whether the employee currently has this equipment available
    /// </summary>
    public bool IsAvailable { get; set; } = true;
}
