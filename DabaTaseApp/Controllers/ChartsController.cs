using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using DabaTaseApp.Models;
using DabaTaseApp.Security;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DabaTaseApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = AppPolicies.Statistics)]
    public class ChartsController : ControllerBase
    {
        private readonly Lab1Context _context;

        public ChartsController(Lab1Context context)
        {
            _context = context;
        }

        [HttpGet("studentsByCategory")]
        public async Task<IActionResult> GetStudentsByCategory()
        {
            var data = await _context.Students
                .GroupBy(s => s.TargetCategory)
                .Select(g => new { label = g.Key, value = g.Count() })
                .ToListAsync();
            return Ok(data);
        }

        [HttpGet("sessionsByInstructor")]
        public async Task<IActionResult> GetSessionsByInstructor([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            if (!IsValidDateRange(startDate, endDate))
            {
                return BadRequest(new { message = "Дата початку не може бути пізніше дати закінчення." });
            }

            var query = _context.PracticeSessions.Include(p => p.Instructor).AsQueryable();

            if (startDate.HasValue) query = query.Where(p => p.StartTime >= startDate.Value);
            if (endDate.HasValue) query = query.Where(p => p.StartTime < endDate.Value.Date.AddDays(1));

            var data = await query
                .GroupBy(p => p.Instructor.FullName)
                .Select(g => new { label = g.Key, value = g.Count() })
                .ToListAsync();
            return Ok(data);
        }

        [HttpGet("revenueByDate")]
        public async Task<IActionResult> GetRevenueByDate([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            if (!IsValidDateRange(startDate, endDate))
            {
                return BadRequest(new { message = "Дата початку не може бути пізніше дати закінчення." });
            }

            var query = _context.Payments.AsQueryable();

            if (startDate.HasValue) query = query.Where(p => p.PaymentDate >= startDate.Value);
            if (endDate.HasValue) query = query.Where(p => p.PaymentDate < endDate.Value.Date.AddDays(1));

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

            return Ok(data);
        }

        private static bool IsValidDateRange(DateTime? startDate, DateTime? endDate)
        {
            return !startDate.HasValue || !endDate.HasValue || startDate.Value.Date <= endDate.Value.Date;
        }
    }
}
