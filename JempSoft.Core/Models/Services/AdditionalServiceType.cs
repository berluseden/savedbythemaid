using System;
using System.Collections.Generic;
using System.Text;

namespace JempSoft.Core.Models.Services
{
    public class AdditionalServiceType : Audits
    {
        public int AdditionalServiceTypeId { get; set; }

        public string Title { get; set; }

        public double Cost { get; set; }

        public double Price { get; set; }

        public string FullDescription { get { return string.Format("{0} - $USD {1}", Title, string.Format("{0:n}", Price)); } }

    }
}
