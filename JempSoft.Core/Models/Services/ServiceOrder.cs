using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace JempSoft.Core.Models
{
    public class ServiceOrder
    {
        [Key]
        public long Id { get; set; }
        
        public int CartItemId { get; set; }

        public int ServiceContactInfoId { get; set; }

        public int Day { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        public int Hour { get; set; }

        public int Minute { get; set; }

        public bool IsPayed { get; set; }

        public bool IsActive { get; set; }
        public bool IsComplete { get; set; }
        public decimal Amount { get; set; }
        public decimal Tax { get; set; }
        public decimal TotalAmount { get; set; }

        [ForeignKey("CartItemId")]
        public CartItem CartItem { get; set; }

        [ForeignKey("ServiceContactInfoId")]
        public ServiceOrderContactInfo ServiceContactInfo { get; set; }

        public virtual ICollection<ServiceOrderAdditionalService> AdditionalServices { get; set; }

    }

    public class ServiceOrderAdditionalService
    {
        public long Id { get; set; }
        
        public long ServiceOrderId { get; set; }

        public int AdditionalServiceId { get; set; }

        [ForeignKey("ServiceOrderId")]
        public virtual ServiceOrder ServiceOrder { get; set; }
    }
}
