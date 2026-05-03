namespace DabaTaseApp.Security;

public static class AppRoles
{
    public const string Admin = "admin";
    public const string Instructor = "instructor";
    public const string Student = "student";

    public const string AdminOrInstructor = "admin,instructor";
    public const string AdminOrStudent = "admin,student";
    public const string AllAuthenticated = "admin,instructor,student";

    public static readonly string[] All = [Admin, Instructor, Student];
}

public static class AppPolicies
{
    public const string Administration = "Administration";
    public const string Management = "Management";
    public const string StudentsAndAdmins = "StudentsAndAdmins";
    public const string Statistics = "Statistics";
}
