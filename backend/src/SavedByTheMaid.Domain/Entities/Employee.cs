using SavedByTheMaid.Domain.Common;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Domain.Entities;

public class Employee : BaseAuditableEntity
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;

    // Relationship with Identity User (optional)
    public string? UserId { get; set; }
    public virtual ApplicationUser? User { get; set; }

    // Primary service area
    public int? PrimaryServiceAreaId { get; set; }
    public virtual ServiceArea? PrimaryServiceArea { get; set; }

    // Operational limits
    public int? MaxDailyHours { get; set; } = 8;
    public int? MaxDailyServices { get; set; } = 4;

    // Navigation
    public virtual ICollection<EmployeeServiceArea> ServiceAreas { get; set; } = new List<EmployeeServiceArea>();
    public virtual ICollection<EmployeeSchedule> Schedules { get; set; } = new List<EmployeeSchedule>();
    public virtual ICollection<EmployeeEquipment> Equipment { get; set; } = new List<EmployeeEquipment>();
    public virtual ICollection<EmployeeTimeOff> TimeOffs { get; set; } = new List<EmployeeTimeOff>();
    public virtual ICollection<ServiceMeet> Meetings { get; set; } = new List<ServiceMeet>();
    public virtual ICollection<SoftReserve> SoftReserves { get; set; } = new List<SoftReserve>();

    public string FullName => $"{FirstName} {LastName}";

    // Optimistic concurrency token — incremented by the caller on every update.
    // Uses ConcurrencyCheck because MySql.EntityFrameworkCore does not support rowversion/timestamp columns.
    [System.ComponentModel.DataAnnotations.ConcurrencyCheck]
    public int Version { get; set; } = 0;
}
