using System;

namespace JempSoft.Applications
{
    public class EmployeeScheduleInputDto
    {
        public int EmployeeId { get; set; }
        public DateTime AvaliableDay { get; set; }
        public bool IsActive { get; set; }
    }

    public class EmployeeScheduleOutputDto
    {
        public int EmployeeScheduleId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime AvaliableDay { get; set; }
        public bool IsActive { get; set; }
    }
}
