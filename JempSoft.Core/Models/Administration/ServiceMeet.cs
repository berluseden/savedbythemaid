using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace JempSoft.Core.Models
{
    public class ServiceMeet
    {
        public int ServiceMeetId { get; set; }

        public int CartItemId { get; set; }

        public string Title { get; set; }

        public string Address { get; set; }

        public int Day { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        public int Hour { get; set; }

        public int Minute { get; set; }

        public bool isMorning { get; set; }

        [ForeignKey("CartItemId")]
        public virtual CartItem CartItem { get; set; }

        public virtual ICollection<Employee> Maids { get; set; }

    }

    public class EmployeeMeetService {

        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public int ServiceMeetId { get; set; }
        
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }

        [ForeignKey("ServiceMeetId")]
        public virtual ServiceMeet MeetService { get; set; }


    }
}
