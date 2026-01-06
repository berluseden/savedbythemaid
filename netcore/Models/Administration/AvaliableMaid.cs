using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace netcore.Models
{
    public class AvaliableMaid
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Maids Avaliables")]
        [Required]
        public int AvaliableCount { get; set; }

        [Display(Name = "Services Day ")]
        public int ServiceCount { get; set; }

        public DateTime DayOfAvaliability { get; set; }
    }


    public class AvaliableMaidHour
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeScheduleId { get; set; }
        
        public DateTime Day { get; set; }

        public TimeSpan Time { get; set; }

        public bool IsActive { get; set; }

        [ForeignKey("EmployeeScheduleId")]
        public virtual EmployeeSchedule EmployeeSchedule { get; set; }

    }
}
