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
    
    // Datos geográficos del ZIP (para display y mapas)
    public string? City { get; set; }
    public string? State { get; set; }
    public string? County { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    
    // Reglas de negocio específicas por ZIP
    public bool IsFullCoverage { get; set; } = true;
    public decimal? SurchargeAmount { get; set; }
    public int? MinimumMinutes { get; set; }
    public string? AvailableDaysJson { get; set; }  // ["Monday","Wednesday","Friday"]
    public string? Notes { get; set; }
    
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
