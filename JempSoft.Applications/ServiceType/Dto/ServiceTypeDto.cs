using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JempSoft.Applications
{
    public class ServiceTypeInputDto
    {
        public string Title { get; set; }

        public double Cost { get; set; }

        public double Price { get; set; }

        public bool IsActive { get; set; }

        public int CreatorUserId { get; set; }
    }

    public class ServiceTypeOutputDto
    {
        public int ServiceTypeId { get; set; }

        public string Title { get; set; }

        public double Cost { get; set; }

        public double Price { get; set; }

        public string FullDescription { get; set; }        

        public bool IsActive { get; set; }

        public string CreatorUserName { get; set; }
    }
}
