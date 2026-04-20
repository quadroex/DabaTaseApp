using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DabaTaseApp.Models;
using System;
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
                .Select(g => new { label = g.Key, value = g.Count() })
                .ToListAsync();
            return new JsonResult(data);
        }

        [HttpGet("sessionsByInstructor")]
        public async Task<JsonResult> GetSessionsByInstructor([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var query = _context.PracticeSessions.Include(p => p.Instructor).AsQueryable();

            if (startDate.HasValue) query = query.Where(p => p.StartTime >= startDate.Value);
            if (endDate.HasValue) query = query.Where(p => p.StartTime <= endDate.Value);

            var data = await query
                .GroupBy(p => p.Instructor.FullName)
                .Select(g => new { label = g.Key, value = g.Count() })
                .ToListAsync();
            return new JsonResult(data);
        }

        [HttpGet("revenueByDate")]
        public async Task<JsonResult> GetRevenueByDate([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var query = _context.Payments.AsQueryable();

            if (startDate.HasValue) query = query.Where(p => p.PaymentDate >= startDate.Value);
            if (endDate.HasValue) query = query.Where(p => p.PaymentDate <= endDate.Value);

            var rawData = await query
                .GroupBy(p => p.PaymentDate.Date)
                .Select(g => new {
                    RawDate = g.Key,
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .OrderBy(d => d.RawDate)
                .ToListAsync();

            var data = rawData.Select(d => new {
                label = d.RawDate.ToString("dd.MM.yyyy"),
                value = (double)d.TotalAmount
            });

            return new JsonResult(data);
        }
    }
}