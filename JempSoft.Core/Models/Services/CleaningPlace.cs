using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace JempSoft.Core.Models
{
    /// <summary>
    /// Place to clean, Studio, House, Building
    /// </summary>
    public class CleaningPlace : Audits
    {
        public int CleaningPlaceId { get; set; }
        public string Title { get; set; }

        [NotMapped]
        public virtual ICollection<CleaningPlaceRoom> CleaningPlaceRooms { get; set; }
    }
}
