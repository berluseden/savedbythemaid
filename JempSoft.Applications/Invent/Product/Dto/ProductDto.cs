using JempSoft.Core.Models.Invent;

namespace JempSoft.Applications.Invent.Dto
{
    public class ProductInputDto
    {
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public ProductType ProductType { get; set; }
        public UOM Uom { get; set; }
    }

    public class ProductOutputDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public ProductType ProductType { get; set; }
        public UOM Uom { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
