using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace netcore.Models
{
    public class OrderItem
    {
        public int OrderItemId { get; set; }


        public int CartItemId { get; set; }

        public int AdditionalServiceTypeId { get; set; }

        public int Qty { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Address { get; set; }

        public string ContactNumber { get; set; }

        [ForeignKey("CartItemId")]
        public virtual CartItem CartItem { get; set; }

        [ForeignKey("AdditionalServiceTypeId")]
        public virtual AdditionalServiceType AdditionalServiceType { get; set; }
    }
}
