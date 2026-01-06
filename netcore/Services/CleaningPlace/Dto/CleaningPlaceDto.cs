using netcore.Models;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace netcore.Dto
{
    public class CleaningPlaceInputDto
    {
        public int CleaningPlaceId { get; set; }

        public string Title { get; set; }

        public bool IsActive { get; set; }

        public int CreateUserId { get; set; }
    }

    public class CleaningPlaceOutDto
    {
        public int CleaningPlaceId { get; set; }

        public string Title { get; set; }

        public bool IsActive { get; set; }

        public string CreateUserName { get; set; }
    }
}
