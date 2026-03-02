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
    public class PracticeSessionsController : Controller
    {
        private readonly Lab1Context _context;

        public PracticeSessionsController(Lab1Context context)
        {
            _context = context;
        }

        // GET: PracticeSessions
        public async Task<IActionResult> Index()
        {
            var lab1Context = _context.PracticeSessions.Include(p => p.Instructor).Include(p => p.Student).Include(p => p.VehiclePlateNavigation);
            return View(await lab1Context.ToListAsync());
        }

        // GET: PracticeSessions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var practiceSession = await _context.PracticeSessions
                .Include(p => p.Instructor)
                .Include(p => p.Student)
                .Include(p => p.VehiclePlateNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (practiceSession == null)
            {
                return NotFound();
            }

            return View(practiceSession);
        }

        // GET: PracticeSessions/Create
        public IActionResult Create()
        {
            ViewData["InstructorId"] = new SelectList(_context.Instructors, "Id", "Id");
            ViewData["StudentId"] = new SelectList(_context.Students, "Id", "Id");
            ViewData["VehiclePlate"] = new SelectList(_context.Vehicles, "PlateNumber", "PlateNumber");
            return View();
        }

        // POST: PracticeSessions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,StartTime,EndTime,Status,InstructorId,StudentId,VehiclePlate")] PracticeSession practiceSession)
        {
            if (ModelState.IsValid)
            {
                _context.Add(practiceSession);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["InstructorId"] = new SelectList(_context.Instructors, "Id", "Id", practiceSession.InstructorId);
            ViewData["StudentId"] = new SelectList(_context.Students, "Id", "Id", practiceSession.StudentId);
            ViewData["VehiclePlate"] = new SelectList(_context.Vehicles, "PlateNumber", "PlateNumber", practiceSession.VehiclePlate);
            return View(practiceSession);
        }

        // GET: PracticeSessions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var practiceSession = await _context.PracticeSessions.FindAsync(id);
            if (practiceSession == null)
            {
                return NotFound();
            }
            ViewData["InstructorId"] = new SelectList(_context.Instructors, "Id", "Id", practiceSession.InstructorId);
            ViewData["StudentId"] = new SelectList(_context.Students, "Id", "Id", practiceSession.StudentId);
            ViewData["VehiclePlate"] = new SelectList(_context.Vehicles, "PlateNumber", "PlateNumber", practiceSession.VehiclePlate);
            return View(practiceSession);
        }

        // POST: PracticeSessions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StartTime,EndTime,Status,InstructorId,StudentId,VehiclePlate")] PracticeSession practiceSession)
        {
            if (id != practiceSession.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(practiceSession);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PracticeSessionExists(practiceSession.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["InstructorId"] = new SelectList(_context.Instructors, "Id", "Id", practiceSession.InstructorId);
            ViewData["StudentId"] = new SelectList(_context.Students, "Id", "Id", practiceSession.StudentId);
            ViewData["VehiclePlate"] = new SelectList(_context.Vehicles, "PlateNumber", "PlateNumber", practiceSession.VehiclePlate);
            return View(practiceSession);
        }

        // GET: PracticeSessions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var practiceSession = await _context.PracticeSessions
                .Include(p => p.Instructor)
                .Include(p => p.Student)
                .Include(p => p.VehiclePlateNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (practiceSession == null)
            {
                return NotFound();
            }

            return View(practiceSession);
        }

        // POST: PracticeSessions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var practiceSession = await _context.PracticeSessions.FindAsync(id);
            if (practiceSession != null)
            {
                _context.PracticeSessions.Remove(practiceSession);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PracticeSessionExists(int id)
        {
            return _context.PracticeSessions.Any(e => e.Id == id);
        }
    }
}
