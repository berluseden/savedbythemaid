using netcore.Models;
﻿using System;
using System.Collections.Generic;
using System.Text;

namespace netcore.Services.Book.Dto
{
    public class OrderToCheckOutDto
    {
        public OrderToCheckOutDto()
        {
            AdditionalServiceTypes = new List<AddtionalServiceTypeListOutputDto>();
        }

        public int CartItemId { get; set; }

        public string CleaningPlace_Title { get; set; }

        public string CleaningPlaceRoom_Title { get; set; }

        public string ServiceType_Title { get; set; }

        public decimal ServiceType_Price { get; set; }

        public int OrderItemQty { get; set; }

        public DateTime DayOfService { get; set; }

        public int HourOfService { get; set; }

        public bool IsHalf { get; set; }

        public List<AddtionalServiceTypeListOutputDto> AdditionalServiceTypes { get; set; }
    }

    public class AvaliableOutPut {

        public int AvaliableMaids { get; set; }

        public DateTime Day { get; set; }
    }
}
