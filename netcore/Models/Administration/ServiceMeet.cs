using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace netcore.Models
{
    public class ServiceMeet
    {
        public int ServiceMeetId { get; set; }

        public int CartItemId { get; set; }

        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        // Campos legacy (mantener por compatibilidad)
        public int Day { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public bool isMorning { get; set; }

        // === Nuevos campos MVP ===

        /// <summary>
        /// Inicio programado del servicio
        /// </summary>
        public DateTime? ScheduledStart { get; set; }

        /// <summary>
        /// Fin programado del servicio
        /// </summary>
        public DateTime? ScheduledEnd { get; set; }

        /// <summary>
        /// Inicio real del servicio
        /// </summary>
        public DateTime? ActualStart { get; set; }

        /// <summary>
        /// Fin real del servicio
        /// </summary>
        public DateTime? ActualEnd { get; set; }

        /// <summary>
        /// Empleada asignada a este servicio
        /// </summary>
        public int? AssignedEmployeeId { get; set; }

        /// <summary>
        /// Estado de la cita
        /// </summary>
        public MeetStatus MeetStatus { get; set; } = MeetStatus.Scheduled;

        /// <summary>
        /// Notas del servicio
        /// </summary>
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // === Navigation properties ===

        [ForeignKey("CartItemId")]
        public virtual CartItem CartItem { get; set; } = null!;

        [ForeignKey("AssignedEmployeeId")]
        public virtual Employee? AssignedEmployee { get; set; }

        public virtual ICollection<Employee> Maids { get; set; } = new List<Employee>();
    }

    public class EmployeeMeetService 
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public int ServiceMeetId { get; set; }
        
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;

        [ForeignKey("ServiceMeetId")]
        public virtual ServiceMeet MeetService { get; set; } = null!;
    }
}
