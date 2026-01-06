using netcore.Models;
﻿namespace netcore.Services.Book.Dto
{
    public class CartItemOutPutDto
    {
        public int CartItemId { get; set; }

        public int CleaningPlaceId { get; set; }

        public string CleaningPlaceTitle { get; set; }

        public int CleaningPlaceRoomId { get; set; }
        public string CleaningPlaceRoomTitle { get; set; }

        public int ServiceTypeId { get; set; }
        public string ServiceTypeFullDescription { get; set; }

        public double Price { get; set; }

    }
}