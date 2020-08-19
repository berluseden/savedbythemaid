using System.Security.Claims;

namespace netcore.Extensions
{
    public static class IdentityUserExtension
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            return 1;
        }
    }
}
