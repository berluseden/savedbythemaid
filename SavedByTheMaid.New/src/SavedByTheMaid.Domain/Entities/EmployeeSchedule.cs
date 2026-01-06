using SavedByTheMaid.Domain.Common;
using SavedByTheMaid.Domain.Enums;

namespace SavedByTheMaid.Domain.Entities;

/// <summary>
/// Horario de disponibilidad de una empleada
/// </summary>
public class EmployeeSchedule : BaseEntity
{
    public int EmployeeId { get; set; }
    public virtual Employee? Employee { get; set; }

    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; } = new TimeSpan(8, 0, 0);  // 08:00
    public TimeSpan EndTime { get; set; } = new TimeSpan(18, 0, 0);   // 18:00
    
    /// <summary>
    /// Minutos de buffer entre citas
    /// </summary>
    public int BufferMinutes { get; set; } = 30;

    public bool IsAvailable { get; set; } = true;
}
