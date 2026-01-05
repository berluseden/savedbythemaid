namespace JempSoft.Applications.Invent.Dto
{
    public class BranchInputDto
    {
        public string BranchName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Street1 { get; set; } = string.Empty;
        public string Street2 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public bool IsDefaultBranch { get; set; }
    }

    public class BranchOutputDto
    {
        public string BranchId { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Street1 { get; set; } = string.Empty;
        public string Street2 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public bool IsDefaultBranch { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
