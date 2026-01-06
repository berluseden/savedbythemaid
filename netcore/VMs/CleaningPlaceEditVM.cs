using JempSoft.Core.Models;
using System.Collections.Generic;

namespace netcore.VMs
{
    /// <summary>
    /// ViewModel para editar un Tipo de Inmueble con sus habitaciones asociadas
    /// </summary>
    public class CleaningPlaceEditVM
    {
        public CleaningPlaceEditVM()
        {
            AvailableRooms = new List<RoomSelectionItem>();
        }

        public int CleaningPlaceId { get; set; }
        public string Title { get; set; }
        public bool IsActive { get; set; }
        public int CreatorUserId { get; set; }
        public System.DateTime CreationDate { get; set; }

        /// <summary>
        /// Lista de todas las habitaciones con su estado de selección
        /// </summary>
        public List<RoomSelectionItem> AvailableRooms { get; set; }
    }

    public class RoomSelectionItem
    {
        public int CleaningPlaceRoomId { get; set; }
        public string Title { get; set; }
        public bool IsSelected { get; set; }
        public bool IsActive { get; set; }
    }
}
