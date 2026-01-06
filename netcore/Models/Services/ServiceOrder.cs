using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace netcore.Models
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

        [Obsolete("Use PaymentStatus instead")]
        public bool IsPayed { get; set; }

        public bool IsActive { get; set; }

        [Obsolete("Use OrderStatus instead")]
        public bool IsComplete { get; set; }

        public decimal Amount { get; set; }
        public decimal Tax { get; set; }
        public decimal TotalAmount { get; set; }

        // === Nuevos campos MVP ===

        /// <summary>
        /// Zona de servicio determinada por ZIP
        /// </summary>
        public int? ServiceAreaId { get; set; }

        /// <summary>
        /// Código postal del cliente
        /// </summary>
        [MaxLength(10)]
        public string? ZipCode { get; set; }

        /// <summary>
        /// Estado del pago
        /// </summary>
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        /// <summary>
        /// Estado de la orden
        /// </summary>
        public OrderStatus OrderStatus { get; set; } = OrderStatus.Draft;

        /// <summary>
        /// Tipo de recurrencia
        /// </summary>
        public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.Once;

        /// <summary>
        /// Origen de la orden
        /// </summary>
        public OrderSource Source { get; set; } = OrderSource.Web;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // === Navigation properties ===

        [ForeignKey("CartItemId")]
        public CartItem CartItem { get; set; } = null!;

        [ForeignKey("ServiceContactInfoId")]
        public ServiceOrderContactInfo ServiceContactInfo { get; set; } = null!;

        [ForeignKey("ServiceAreaId")]
        public virtual ServiceArea? ServiceArea { get; set; }

        public virtual ICollection<ServiceOrderAdditionalService> AdditionalServices { get; set; } = new List<ServiceOrderAdditionalService>();
    }

    public class ServiceOrderAdditionalService
    {
        public long Id { get; set; }
        
        public long ServiceOrderId { get; set; }

        public int AdditionalServiceId { get; set; }

        [ForeignKey("ServiceOrderId")]
        public virtual ServiceOrder ServiceOrder { get; set; } = null!;
    }
}
