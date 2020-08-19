using System;
using System.Collections.Generic;
using System.Text;

namespace JempSoft.Applications.Book.Dto
{
    public class ServiceContactInfoInputDto
    {
        public string Name { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string Address { get; set; }

        public string AdditionalServiceInfo { get; set; }
    }

    public class ServiceOrderInputDto
    {
        //int cartItemId, int day, int month, int year, int hour, int minute, List<int> aditionalServices

        public int CartItemId { get; set; }

        public string Email { get; set; }

        public int Day { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        public int Hour { get; set; }

        public int Minute { get; set; }

        List<int> AdditionalServices { get; set; }

        public bool IsPayed { get; set; }

        public bool IsComplete { get; set; }

        public bool IsActive { get; set; }

        public decimal Amount { get; set; }

        public decimal Tax { get; set; }

        public decimal TotalAmount { get; set; }
    }

    public class ServiceOrderOutputDto
    {

        public ServiceOrderOutputDto()
        {
            ServiceTypes = new List<ServiceTypeOutputDto>();
            AdditionalServices = new List<AddtionalServiceTypeListOutputDto>();
        }


        public int CartItemId { get; set; }
        
        public int Day { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        public int Hour { get; set; }

        public int Minute { get; set; }

        public List<ServiceTypeOutputDto> ServiceTypes { get; set; }

        public List<AddtionalServiceTypeListOutputDto> AdditionalServices { get; set; }
    }

    public class ServiceOrderAdditionalServiceInputDto
    {
        public int ServiceOrderId { get; set; }

        public int AdditionalServiceTypeId { get; set; }
    }
}
