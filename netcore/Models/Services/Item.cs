using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace netcore.Models
{
    public class CartItem
    {
        [Key]
        public int CartItemId { get; set; }

        public int CleaningPlaceId { get; set; }

        public int CleaningPlaceRoomId { get; set; }

        public int ServiceTypeId { get; set; }


        //[ForeignKey("CleaningPlaceId")]
        //public virtual CleaningPlace CleaningPlace { get; set; }

        //[ForeignKey("CleaningPlaceRoomId")]
        //public virtual CleaningPlaceRoom CleaningPlaceRoom { get; set; }

        //[ForeignKey("ServiceTypeId")]
        //public virtual ServiceType ServiceType { get; set; }

    }
}
