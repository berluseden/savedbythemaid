using JempSoft.Core.Models.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace JempSoft.Applications.Book.Dto
{

    public class ServiceItemsOnCartDto
    {
        public ServiceItemsOnCartDto()
        {
            Services = new List<ServicesOnCartDto>();
            AdditionalServices = new List<AdditionalServiceType>();
            Footer = new ServicesFooterOnDto();
        }


        public List<ServicesOnCartDto> Services { get; set; }
       
        public List<AdditionalServiceType> AdditionalServices { get; set; }
        public ServicesFooterOnDto Footer { get; set; }
    }

    public class ServicesOnCartDto
    {
        public ServicesOnCartDto()
        {
            AdditionalServicesOnCart = new List<AdditionalServiceType>();
        }

        public long ServiceId { get; set; }
        public long CartItemId { get; set; }
        public string CleaningPlace { get; set; }
        public string CleaningPlaceRoom { get; set; }
        public string ServiceType { get; set; }
        public List<AdditionalServiceType> AdditionalServicesOnCart { get; set; }
        public decimal ServiceTypePrice { get; set; }
        public decimal OrderAmount { get; set; }
        public double OrderTax { get; set; }
        public decimal OrderTotalAmount { get; set; }
    }

    public class AdditionalServiceTypeOnCartDto
    {
        public AdditionalServiceTypeOnCartDto()
        {
            AdditionalServices = new List<AdditionalServiceType>();
        }

        public List<AdditionalServiceType> AdditionalServices { get; set; }
    }

    public class ServicesFooterOnDto
    {
        public decimal SubTotal { get; set; }
        public decimal Tax { get; set; }
        public decimal SubItemsTotal { get; set; }
        public decimal Total { get; set; }
    }
}
