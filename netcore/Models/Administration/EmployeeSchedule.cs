using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace netcore.Models
{
    public class EmployeeSchedule
    {
        public int EmployeeScheduleId { get; set; }

        public int EmployeeId { get; set; }

        /// <summary>
        /// Día disponible (solo se usa la fecha, no la hora)
        /// </summary>
        public DateTime AvaliableDay { get; set; }

        /// <summary>
        /// Hora de inicio de disponibilidad
        /// </summary>
        public TimeSpan StartTime { get; set; } = new TimeSpan(8, 0, 0); // 08:00

        /// <summary>
        /// Hora de fin de disponibilidad
        /// </summary>
        public TimeSpan EndTime { get; set; } = new TimeSpan(18, 0, 0); // 18:00

        /// <summary>
        /// Minutos de buffer entre servicios (tiempo de traslado)
        /// </summary>
        public int BufferMinutes { get; set; } = 30;

        public bool IsActive { get; set; }

        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;
    }
}
