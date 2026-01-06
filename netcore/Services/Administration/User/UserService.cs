using netcore.Data;

namespace netcore.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public User RegisterUser(string userName, bool isActive)
        {
            try
            {
                var user = new User
                {
                    UserName = userName,
                    IsActive = isActive,
                    CreateDate = DateTime.UtcNow
                };

                // Note: User table not in current DbContext, just return the object
                // _context.Add(user);
                // _context.SaveChanges();

                return user;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public int GetUserId(string userName)
        {
            // Note: User table not in current DbContext
            return 0;
        }
    }
}
