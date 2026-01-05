using JempSoft.Core.Models.Invent;

namespace JempSoft.Applications.Invent.Dto
{
    public class CustomerInputDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public BusinessSize Size { get; set; }
        public string Street1 { get; set; } = string.Empty;
        public string Street2 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }

    public class CustomerOutputDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
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
