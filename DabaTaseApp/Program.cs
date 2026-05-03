using DabaTaseApp.Models;
using DabaTaseApp.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys")));

builder.Services.AddDbContext<Lab1Context>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;

        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<Lab1Context>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true;
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppPolicies.Administration, policy =>
        policy.RequireRole(AppRoles.Admin));
    options.AddPolicy(AppPolicies.Management, policy =>
        policy.RequireRole(AppRoles.Admin, AppRoles.Instructor));
    options.AddPolicy(AppPolicies.StudentsAndAdmins, policy =>
        policy.RequireRole(AppRoles.Admin, AppRoles.Student));
    options.AddPolicy(AppPolicies.Statistics, policy =>
        policy.RequireRole(AppRoles.Admin));
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

try
{
    await SeedIdentityAsync(app);
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Database initialization was skipped. Check the PostgreSQL connection string and server availability.");
}

app.MapRazorPages();
app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

static async Task SeedIdentityAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider.GetRequiredService<Lab1Context>();
    await db.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    foreach (var role in AppRoles.All)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    await ReconcileProfileLinksAsync(db, userManager);

    if (!app.Environment.IsDevelopment())
    {
        return;
    }

    await EnsureUserAsync(userManager, "admin@test.com", "Admin!2026", AppRoles.Admin);
    await EnsureUserAsync(userManager, "inst@test.com", "Instructor!2026", AppRoles.Instructor);

    var demoStudentUser = await EnsureUserAsync(userManager, "stud@test.com", "Student!2026", AppRoles.Student);
    await EnsureDemoStudentAsync(db, demoStudentUser);
}

static async Task<IdentityUser> EnsureUserAsync(
    UserManager<IdentityUser> userManager,
    string email,
    string password,
    string role)
{
    var user = await userManager.FindByEmailAsync(email);
    if (user == null)
    {
        user = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not create demo user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    if (!await userManager.IsInRoleAsync(user, role))
    {
        await userManager.AddToRoleAsync(user, role);
    }

    return user;
}

static async Task EnsureDemoStudentAsync(Lab1Context db, IdentityUser user)
{
    var existingLinkedStudent = await db.Students.FirstOrDefaultAsync(s => s.ApplicationUserId == user.Id);
    if (existingLinkedStudent != null)
    {
        return;
    }

    var targetCategory = await db.Categories
        .OrderBy(c => c.Name)
        .Select(c => c.Name)
        .FirstOrDefaultAsync() ?? "B";

    var existingStudent = await db.Students
        .FirstOrDefaultAsync(s => s.FullName == "Тестовий учень" || s.FullName == user.Email);

    if (existingStudent == null)
    {
        db.Students.Add(new Student
        {
            ApplicationUserId = user.Id,
            FullName = "Тестовий учень",
            Balance = 0,
            TargetCategory = targetCategory
        });
    }
    else
    {
        existingStudent.ApplicationUserId = user.Id;
    }

    await db.SaveChangesAsync();
}

static async Task ReconcileProfileLinksAsync(Lab1Context db, UserManager<IdentityUser> userManager)
{
    var linkedStudents = await db.Students
        .Where(s => s.ApplicationUserId != null)
        .ToListAsync();

    foreach (var student in linkedStudents)
    {
        var user = await userManager.FindByIdAsync(student.ApplicationUserId!);
        if (user != null && await userManager.IsInRoleAsync(user, AppRoles.Student))
        {
            continue;
        }

        var hasHistory = student.Balance != 0
            || student.GroupId != null
            || await db.Payments.AnyAsync(p => p.StudentId == student.Id)
            || await db.PracticeSessions.AnyAsync(p => p.StudentId == student.Id);

        if (!hasHistory && LooksLikeEmail(student.FullName))
        {
            db.Students.Remove(student);
        }
        else
        {
            student.ApplicationUserId = null;
        }
    }

    await db.SaveChangesAsync();
}

static bool LooksLikeEmail(string value)
{
    return value.Contains('@') && value.Contains('.');
}
