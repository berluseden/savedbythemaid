using JempSoft.Core.Models;

namespace JempSoft.Applications
{
    public interface IUserService
    {
        User RegisterUser(string userName, bool isActive);

        int GetUserId(string userName);
    }
}