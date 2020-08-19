using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace JempSoft.Core.Models
{
    public class EmployeeSchedule
    {
        public int EmployeeScheduleId { get; set; }

        public int EmployeeId { get; set; }

        public DateTime AvaliableDay { get; set; }

        public bool IsActive { get; set; }

        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }

    }

}
