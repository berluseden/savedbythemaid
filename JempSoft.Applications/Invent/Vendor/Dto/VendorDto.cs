using JempSoft.Core.Models.Invent;

namespace JempSoft.Applications.Invent.Dto
{
    public class VendorInputDto
    {
        public string VendorName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public BusinessSize Size { get; set; }
        public string Street1 { get; set; } = string.Empty;
        public string Street2 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }

    public class VendorOutputDto
    {
        public string VendorId { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public BusinessSize Size { get; set; }
        public string Street1 { get; set; } = string.Empty;
        public string Street2 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
