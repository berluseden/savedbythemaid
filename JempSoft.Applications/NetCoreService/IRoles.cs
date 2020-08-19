using JempSoft.Core.Models;
using System.Threading.Tasks;

namespace JempSoft.Applications.NetServices
{
    public interface IRoles
    {
        Task UpdateRoles(ApplicationUser appUser, ApplicationUser currentUserLogin);
    }
}
