using JempSoft.Core.Models.Invent;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace JempSoft.Core.Models
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

        [NotMapped]
        public virtual ICollection<EmployeeSchedule> Schedules { get; set; }
    }
}
