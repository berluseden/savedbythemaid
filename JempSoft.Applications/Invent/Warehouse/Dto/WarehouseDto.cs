namespace JempSoft.Applications.Invent.Dto
{
    public class WarehouseInputDto
    {
        public string BranchId { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Street1 { get; set; } = string.Empty;
        public string Street2 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }

    public class WarehouseOutputDto
    {
        public string WarehouseId { get; set; } = string.Empty;
        public string BranchId { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Street1 { get; set; } = string.Empty;
        public string Street2 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
