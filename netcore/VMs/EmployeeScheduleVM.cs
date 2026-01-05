using JempSoft.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace netcore.VMs
{
    public class EmployeeScheduleVM
    {
        public EmployeeScheduleVM()
        {
            EmployeeSchedule = new List<EmployeeSchedule>();
            Schedule = new EmployeeSchedule();
        }

        public Employee? Employee { get; set; }

        public EmployeeSchedule Schedule { get; set; }

        public List<EmployeeSchedule> EmployeeSchedule { get; set; }        

        public bool EntireYear { get; set; } = false;
    }
}
