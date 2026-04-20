using DabaTaseApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Threading.Tasks;

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
            ViewBag.TotalStudents = await _context.Students.CountAsync();
            ViewBag.ActiveVehicles = await _context.Vehicles.CountAsync(v => v.IsActive);
            ViewBag.TotalRevenue = await _context.Payments.SumAsync(p => (decimal?)p.Amount) ?? 0;
            ViewBag.ActiveSessions = await _context.PracticeSessions.CountAsync(s => s.Status == "Триває" || s.Status == "Заплановано");

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