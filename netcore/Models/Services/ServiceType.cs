using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace netcore.Models
{
    public class ServiceType : Audits
    {
        public int ServiceTypeId { get; set; }

        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public double Cost { get; set; }

        public double Price { get; set; }

        /// <summary>
        /// Duración estimada del servicio en minutos
        /// </summary>
        public int EstimatedMinutes { get; set; } = 60;

        /// <summary>
        /// Descripción detallada del servicio
        /// </summary>
        public string? Description { get; set; }

        [NotMapped]
        public string FullDescription => $"{Title} - $USD {Price:N2}";

        [NotMapped]
        public virtual ICollection<CleaningPlaceRoom> CleaningPlaceRooms { get; set; } = new List<CleaningPlaceRoom>();
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