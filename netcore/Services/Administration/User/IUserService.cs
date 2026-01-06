namespace netcore.Services
{
    public interface IUserService
    {
        User RegisterUser(string userName, bool isActive);
        int GetUserId(string userName);
    }

    public class User
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
