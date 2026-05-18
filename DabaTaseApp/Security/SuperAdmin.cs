using System.Security.Claims;

namespace DabaTaseApp.Security;

public static class SuperAdmin
{
    public const string Email = "superadmin@gmail.com";

    public static bool IsSuperAdmin(ClaimsPrincipal user)
        => user.Identity?.IsAuthenticated == true
           && string.Equals(user.Identity.Name, Email, StringComparison.OrdinalIgnoreCase);
}
