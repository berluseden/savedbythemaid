using netcore.Models;
﻿using System;
using System.Collections.Generic;
using System.Text;

namespace netcore.Services.Book.Dto
{
    public class BookInputDto
    {
        public int CleaningPlaceId { get; set; }

        public int CleaningPlaceRoomId { get; set; }

        public int ServiceTypeId { get; set; }
    }
}
