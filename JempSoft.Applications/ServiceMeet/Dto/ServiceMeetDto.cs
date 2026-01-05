using System;

namespace JempSoft.Applications.ServiceMeet.Dto
{
    public class ServiceMeetInputDto
    {
        public int CartItemId { get; set; }
        public string Title { get; set; }
        public string Address { get; set; }
        public int Day { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public bool IsMorning { get; set; }
    }

    public class ServiceMeetOutputDto
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
        public bool IsMorning { get; set; }
        
        // Navigation property info
        public string CartItemDescription { get; set; }
        
        // Computed property for display
        public DateTime ScheduledDateTime => new DateTime(Year, Month, Day, Hour, Minute, 0);
        public string FormattedDateTime => ScheduledDateTime.ToString("yyyy-MM-dd HH:mm");
    }
}
