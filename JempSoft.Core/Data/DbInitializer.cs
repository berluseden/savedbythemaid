using JempSoft.Core.Models;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;

namespace JempSoft.Core.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(JempSoftDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            context.Database.EnsureCreated();

            //check for users
            if (context.ApplicationUser.Any())
            {
                return; //if user is not empty, DB has been seed
            }            
        }
    }
}
