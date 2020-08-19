using JempSoft.Core.Data;
using JempSoft.Core.Models;
using System;
using System.Linq;

namespace JempSoft.Applications
{
    public class UserService : IUserService
    {
        private readonly JempSoftDbContext _context;

        public UserService(JempSoftDbContext context)
        {
            _context = context;
        }


        public User RegisterUser(string userName, bool isActive)
        {
            try
            {
                var user = new Core.Models.User
                {
                    UserName = userName,
                    IsActive = isActive,
                    CreateDate = DateTime.UtcNow
                };

                _context.Add(user);
                _context.SaveChanges();

                return user;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public int GetUserId (string userName)
        {
            try
            {
                var usr = _context.Users.FirstOrDefault(u => u.UserName.Contains(userName));
                return usr.UserId;
            }
            catch (Exception ex)
            {
                throw new System.Exception(ex.InnerException.Message);
            }
        }
    }
}
