using JempSoft.Core.Models.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EService.Web.VMs
{
    public class CartItemBookVM
    {
        public int CartItemId { get; set; }

        public string ServiceType_FullDescription { get; set; }

        public Schedule Schedule { get; set; }

        public AdditionalServiceType AdditionalService { get; set; }
    }
}
