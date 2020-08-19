using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JempSoft.Core.Models
{
    public class ServiceType : Audits
    {
        public int ServiceTypeId { get; set; }

        public string Title { get; set; }

        public double Cost { get; set; }

        public double Price { get; set; }

        public string FullDescription { get { return string.Format("{0} - $USD {1}", Title, string.Format("{0:n}", Price) ); } }

        [NotMapped]
        public virtual ICollection<CleaningPlaceRoom> CleaningPlaceRooms { get; set; }

    } 

    public class CleaningPlaceRoomServiceType
    {
        [Key]
        public int CleaningPlaceRoomServiceTypeId { get; set; }

        public int CleaningPlaceRoomId { get; set; }

        public int ServiceTypeId { get; set; }

        public bool? IsActive { get; set; }

        //public virtual CleaningPlaceRoom CleaningPlaceRoom { get; set; }
        
        //public virtual ServiceType ServiceType { get; set; }
    }
}