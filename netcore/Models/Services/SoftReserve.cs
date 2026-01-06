using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace netcore.Models
{
    /// <summary>
    /// Reserva temporal para evitar colisiones durante el checkout
    /// Se convierte en reserva confirmada al completar el pago
    /// </summary>
    public class SoftReserve
    {
        [Key]
        public int SoftReserveId { get; set; }

        /// <summary>
        /// ID de sesión para usuarios no autenticados
        /// </summary>
        [MaxLength(100)]
        public string? SessionId { get; set; }

        /// <summary>
        /// ID de cliente para usuarios autenticados
        /// </summary>
        public int? CustomerId { get; set; }

        /// <summary>
        /// Empleada reservada temporalmente
        /// </summary>
        public int EmployeeId { get; set; }

        /// <summary>
        /// Inicio del bloque de tiempo reservado
        /// </summary>
        public DateTime ScheduledStart { get; set; }

        /// <summary>
        /// Fin del bloque de tiempo reservado
        /// </summary>
        public DateTime ScheduledEnd { get; set; }

        /// <summary>
        /// Cuándo expira la reserva temporal (ej: 10 minutos desde creación)
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Estado de la reserva temporal
        /// </summary>
        public SoftReserveStatus Status { get; set; } = SoftReserveStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;
    }

    public enum SoftReserveStatus
    {
        Active = 0,
        Converted = 1,  // Se convirtió en reserva confirmada
        Expired = 2     // Expiró sin convertirse
    }
}
