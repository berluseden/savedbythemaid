namespace JempSoft.Applications
{
    public class AdditionalServiceTypeInputDto
    {
        public string Title { get; set; } = string.Empty;
        public double Cost { get; set; }
        public double Price { get; set; }
        public bool IsActive { get; set; }
        public int CreatorUserId { get; set; }
    }

    public class AdditionalServiceTypeOutputDto
    {
        public int AdditionalServiceTypeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public double Cost { get; set; }
        public double Price { get; set; }
        public string FullDescription { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
