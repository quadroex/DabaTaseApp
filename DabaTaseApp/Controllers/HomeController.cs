using DabaTaseApp.Models;
using DabaTaseApp.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace DabaTaseApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly Lab1Context _context;

        public HomeController(Lab1Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole(AppRoles.Admin))
            {
                ViewBag.TotalStudents = await _context.Students.CountAsync();
                ViewBag.ActiveVehicles = await _context.Vehicles.CountAsync(v => v.IsActive);
                ViewBag.TotalRevenue = await _context.Payments.SumAsync(p => (decimal?)p.Amount) ?? 0;
                ViewBag.ActiveSessions = await _context.PracticeSessions
                    .CountAsync(s => s.Status == "Триває" || s.Status == "Заплановано");
            }

            if (User.IsInRole(AppRoles.Instructor))
            {
                ViewBag.TotalStudents = await _context.Students.CountAsync();
                ViewBag.ActiveVehicles = await _context.Vehicles.CountAsync(v => v.IsActive);
                ViewBag.UpcomingTheorySessions = await _context.TheorySessions.CountAsync(s => s.StartTime >= DateTime.Now);
                ViewBag.UpcomingPracticeSessions = await _context.PracticeSessions.CountAsync(s => s.StartTime >= DateTime.Now);
            }

            if (User.IsInRole(AppRoles.Student))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var student = await _context.Students
                    .Include(s => s.Group)
                    .FirstOrDefaultAsync(s => s.ApplicationUserId == userId);

                ViewBag.Student = student;
                ViewBag.StudentBalance = student?.Balance ?? 0;
                ViewBag.NextPracticeSession = student == null
                    ? null
                    : await _context.PracticeSessions
                        .Include(s => s.Instructor)
                        .Include(s => s.VehiclePlateNavigation)
                        .Where(s => s.StudentId == student.Id && s.StartTime >= DateTime.Now)
                        .OrderBy(s => s.StartTime)
                        .FirstOrDefaultAsync();

                ViewBag.NextTheorySession = student?.GroupId == null
                    ? null
                    : await _context.TheorySessions
                        .Include(s => s.Instructor)
                        .Where(s => s.GroupId == student.GroupId && s.StartTime >= DateTime.Now)
                        .OrderBy(s => s.StartTime)
                        .FirstOrDefaultAsync();
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
