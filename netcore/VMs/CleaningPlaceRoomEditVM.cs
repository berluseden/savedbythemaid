using netcore.Models;
using System.Collections.Generic;

namespace netcore.VMs
{
    /// <summary>
    /// ViewModel para editar una Habitación con sus servicios asociados
    /// </summary>
    public class CleaningPlaceRoomEditVM
    {
        public CleaningPlaceRoomEditVM()
        {
            AvailableServices = new List<ServiceSelectionItem>();
        }

        public int CleaningPlaceRoomId { get; set; }
        public string Title { get; set; }
        public bool IsActive { get; set; }
        public int CreatorUserId { get; set; }
        public System.DateTime CreationDate { get; set; }

        /// <summary>
        /// Lista de todos los servicios con su estado de selección
        /// </summary>
        public List<ServiceSelectionItem> AvailableServices { get; set; }
    }

    public class ServiceSelectionItem
    {
        public int ServiceTypeId { get; set; }
        public string Title { get; set; }
        public string FullDescription { get; set; }
        public decimal Price { get; set; }
        public bool IsSelected { get; set; }
        public bool IsActive { get; set; }
    }
}
