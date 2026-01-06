using netcore.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace netcore.VMs
{
    public class CleaningPlacePlaceRoomsVM
    {
        public CleaningPlacePlaceRoomsVM()
        {
            CleaningPlaceRooms = new List<CleaningPlaceRoom>();
            CleaningPlaceRoomAddeds = new List<CleaningPlaceRoom>();
            CleaningPlace = new CleaningPlace();
        }

        public CleaningPlace CleaningPlace { get; set; }

        [BindProperty]
        [Required]
        public int[] CleaningPlaceRoomIds { get; set; }

        public List<CleaningPlaceRoom> CleaningPlaceRooms { get; set; }

        public List<CleaningPlaceRoom> CleaningPlaceRoomAddeds { get; set; }
    }
}
