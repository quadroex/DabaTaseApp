namespace DabaTaseApp.Security;

public static class AppRoleDisplay
{
    public static string Name(string? role)
    {
        return role switch
        {
            AppRoles.Admin => "адмін",
            AppRoles.Instructor => "інструктор",
            AppRoles.Student => "учень",
            _ => "без ролі"
        };
    }
}
