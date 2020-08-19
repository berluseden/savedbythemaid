using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace JempSoft.Core.Models.Services
{
    public class Schedule
    {
        public int ScheduleId { get; set; }

        public int CalendarId { get; set; }

        public int EmployeeId { get; set; }

        [ForeignKey("CalendarId")]
        public virtual Calendar Calendar { get; set; }

        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }

    }

    public class Calendar {

        public int CalendarId { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public int Day { get; set; }

        public bool IsHoliday { get; set; }

    }
}
