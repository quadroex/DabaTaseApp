using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DabaTaseApp.Models;

namespace DabaTaseApp.Controllers
{
    public class StudentsController : Controller
    {
        private readonly Lab1Context _context;

        public StudentsController(Lab1Context context)
        {
            _context = context;
        }

        // GET: Students
        public async Task<IActionResult> Index(int? id, string? name)
        {
            if (id == null)
            {
                var allStudents = _context.Students.Include(s => s.Group);
                return View(await allStudents.ToListAsync());
            }

            ViewBag.GroupId = id;
            ViewBag.GroupName = name;

            var studentsByGroup = _context.Students.Where(s => s.GroupId == id).Include(s => s.Group);
            return View(await studentsByGroup.ToListAsync());
        }

        // GET: Students/Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.Group)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // GET: Students/Create
        public IActionResult Create(int? categoryId)
        {
            if (categoryId != null)
            {
                ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "GroupName", categoryId);
            }
            else
            {
                ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "GroupName");
            }

            ViewData["TargetCategory"] = new SelectList(_context.Categories, "Name", "Name");
            return View();
        }

        // POST: Students/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FullName,Balance,TargetCategory,GroupId")] Student student)
        {
            ModelState.Remove("Group");
            ModelState.Remove("Payments");
            ModelState.Remove("PracticeSessions");

            if (ModelState.IsValid)
            {
                _context.Add(student);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "GroupName", student.GroupId);
            ViewData["TargetCategory"] = new SelectList(_context.Categories, "Name", "Name");
            return View(student);
        }

        // GET: Students/Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound();

            ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "GroupName", student.GroupId);
            ViewData["TargetCategory"] = new SelectList(_context.Categories, "Name", "Name");
            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Balance,TargetCategory,GroupId")] Student student)
        {
            if (id != student.Id) return NotFound();

            ModelState.Remove("Group");
            ModelState.Remove("Payments");
            ModelState.Remove("PracticeSessions");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(student);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Students.Any(e => e.Id == student.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "GroupName", student.GroupId);
            ViewData["TargetCategory"] = new SelectList(_context.Categories, "Name", "Name");
            return View(student);
        }

        // GET: Students/Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.Group)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // POST: Students/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                try
                {
                    _context.Students.Remove(student);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    TempData["ErrorMessage"] = "Неможливо видалити учня, оскільки у нього є платежі або практичні заняття.";
                    return RedirectToAction(nameof(Delete), new { id = id });
                }
            }
            return RedirectToAction(nameof(Index));
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Id == id);
        }
    }
}
