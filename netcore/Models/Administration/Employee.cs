using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace netcore.Models
{
    public class Employee : Person
    {
        public int EmployeeId { get; set; }

        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public bool IsActive { get; set; } = true;
        
        public DateTime CreationDate { get; set; } = DateTime.Now;
        
        public DateTime? UpdateDate { get; set; }

        /// <summary>
        /// Zona principal de servicio de la empleada
        /// </summary>
        public int? PrimaryServiceAreaId { get; set; }

        [ForeignKey("PrimaryServiceAreaId")]
        public virtual ServiceArea? PrimaryServiceArea { get; set; }

        [NotMapped]
        public virtual ICollection<EmployeeSchedule> Schedules { get; set; } = new List<EmployeeSchedule>();

        /// <summary>
        /// Zonas que cubre esta empleada
        /// </summary>
        public virtual ICollection<EmployeeServiceArea> ServiceAreas { get; set; } = new List<EmployeeServiceArea>();
    }
}
