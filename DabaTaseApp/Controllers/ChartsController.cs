using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DabaTaseApp.Models;
using System.Linq;
using System.Threading.Tasks;

namespace DabaTaseApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChartsController : ControllerBase
    {
        private readonly Lab1Context _context;

        public ChartsController(Lab1Context context)
        {
            _context = context;
        }

        [HttpGet("studentsByCategory")]
        public async Task<JsonResult> GetStudentsByCategory()
        {
            var data = await _context.Students
                .GroupBy(s => s.TargetCategory)
                .Select(g => new { category = g.Key, count = g.Count() })
                .ToListAsync();
            return new JsonResult(data);
        }

        [HttpGet("sessionsByInstructor")]
        public async Task<JsonResult> GetSessionsByInstructor()
        {
            var data = await _context.PracticeSessions
                .Include(p => p.Instructor)
                .GroupBy(p => p.Instructor.FullName)
                .Select(g => new { instructor = g.Key, count = g.Count() })
                .ToListAsync();
            return new JsonResult(data);
        }
    }
}