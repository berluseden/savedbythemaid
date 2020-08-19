using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JempSoft.Core.Models
{
    /// <summary>
    /// Baths, Baseman, Yard, Music Room
    /// </summary>
    public class CleaningPlaceRoom : Audits
    {
        public int CleaningPlaceRoomId { get; set; }

        public string Title { get; set; }

        public virtual ICollection<CleaningPlace> CleaningPlaces { get; set; }

        public virtual ICollection<ServiceType> ServiceTypes { get; set; }
    }

    public class CleaningPlaceCleaningPlaceRoom
    {
        [Key]
        public int Id { get; set; }

        public int CleaningPlaceId { get; set; }

        public int CleaningPlaceRoomId { get; set; }

        public bool IsActive { get; set; }

        [NotMapped]
        public CleaningPlace CleaningPlace { get; set; }

        [NotMapped]
        public CleaningPlaceRoom CleaningPlaceRoom { get; set; }
    }
}