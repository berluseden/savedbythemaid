using SavedByTheMaid.Domain.Common;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Service area - groups zip codes for employee assignment
/// </summary>
public class ServiceArea : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<ServiceAreaZip> ZipCodes { get; set; } = new List<ServiceAreaZip>();
    public virtual ICollection<EmployeeServiceArea> EmployeeAssignments { get; set; } = new List<EmployeeServiceArea>();
}

/// <summary>
/// Zip code to service area association
/// </summary>
public class ServiceAreaZip : BaseEntity
{
    public required string ZipCode { get; set; }
    
    // Geographic data for the ZIP (for display and maps)
    public string? City { get; set; }
    public string? State { get; set; }
    public string? County { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    
    // ZIP-specific business rules
    public bool IsFullCoverage { get; set; } = true;
    public decimal? SurchargeAmount { get; set; }
    public int? MinimumMinutes { get; set; }
    public string? AvailableDaysJson { get; set; }  // ["Monday","Wednesday","Friday"]
    public string? Notes { get; set; }
    
    public int ServiceAreaId { get; set; }
    public virtual ServiceArea? ServiceArea { get; set; }
}

/// <summary>
/// Employee to service area assignment
/// </summary>
public class EmployeeServiceArea : BaseEntity
{
    public int EmployeeId { get; set; }
    public virtual Employee? Employee { get; set; }

    public int ServiceAreaId { get; set; }
    public virtual ServiceArea? ServiceArea { get; set; }

    public bool IsPrimary { get; set; } = false;
}
