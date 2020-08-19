using JempSoft.Core.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace netcore.VMs
{
    public class PlaceRoomsServiceTypesVM
    {
        public PlaceRoomsServiceTypesVM()
        {
            ServiceTypes = new List<ServiceType>();
            ServiceTypesddeds = new List<ServiceType>();
            CleaningPlaceRooms = new CleaningPlaceRoom();
        }

        public CleaningPlaceRoom CleaningPlaceRooms { get; set; } 

        [BindProperty]
        public int[] ServiceTypesIds { get; set; }

        public List<ServiceType> ServiceTypes { get; set; }

        public List<ServiceType> ServiceTypesddeds { get; set; }
    }
}
