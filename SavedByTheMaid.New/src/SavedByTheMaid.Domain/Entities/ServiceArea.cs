using SavedByTheMaid.Domain.Common;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Zona de servicio - agrupa códigos postales para asignar empleadas
/// </summary>
public class ServiceArea : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navegación
    public virtual ICollection<ServiceAreaZip> ZipCodes { get; set; } = new List<ServiceAreaZip>();
    public virtual ICollection<EmployeeServiceArea> EmployeeAssignments { get; set; } = new List<EmployeeServiceArea>();
}

/// <summary>
/// Asociación de código postal a zona de servicio
/// </summary>
public class ServiceAreaZip : BaseEntity
{
    public required string ZipCode { get; set; }
    
    public int ServiceAreaId { get; set; }
    public virtual ServiceArea? ServiceArea { get; set; }
}

/// <summary>
/// Asignación de empleada a zona de servicio
/// </summary>
public class EmployeeServiceArea : BaseEntity
{
    public int EmployeeId { get; set; }
    public virtual Employee? Employee { get; set; }

    public int ServiceAreaId { get; set; }
    public virtual ServiceArea? ServiceArea { get; set; }

    public bool IsPrimary { get; set; } = false;
}
