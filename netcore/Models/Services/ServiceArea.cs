using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace netcore.Models
{
    /// <summary>
    /// Zona de servicio - agrupa códigos postales para asignar empleadas
    /// </summary>
    public class ServiceArea
    {
        [Key]
        public int ServiceAreaId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? State { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<ServiceAreaZip> ZipCodes { get; set; } = new List<ServiceAreaZip>();
        public virtual ICollection<EmployeeServiceArea> EmployeeServiceAreas { get; set; } = new List<EmployeeServiceArea>();
    }

    /// <summary>
    /// Códigos postales por zona - cada ZIP solo puede pertenecer a una zona
    /// </summary>
    public class ServiceAreaZip
    {
        [Key]
        public int Id { get; set; }

        public int ServiceAreaId { get; set; }

        [Required]
        [MaxLength(10)]
        public string ZipCode { get; set; } = string.Empty;

        // Navigation
        public virtual ServiceArea ServiceArea { get; set; } = null!;
    }

    /// <summary>
    /// Relación empleada-zona (una empleada puede cubrir múltiples zonas)
    /// </summary>
    public class EmployeeServiceArea
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public int ServiceAreaId { get; set; }

        /// <summary>
        /// Indica si esta es la zona principal de la empleada
        /// </summary>
        public bool IsPrimary { get; set; } = false;

        // Navigation
        public virtual Employee Employee { get; set; } = null!;
        public virtual ServiceArea ServiceArea { get; set; } = null!;
    }
}
