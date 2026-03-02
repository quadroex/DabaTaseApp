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
    public class TheorySessionsController : Controller
    {
        private readonly Lab1Context _context;

        public TheorySessionsController(Lab1Context context)
        {
            _context = context;
        }

        // GET: TheorySessions
        public async Task<IActionResult> Index()
        {
            var lab1Context = _context.TheorySessions.Include(t => t.Group).Include(t => t.Instructor);
            return View(await lab1Context.ToListAsync());
        }

        // GET: TheorySessions/Details/5
        public async Task<IActionResult> Details(DateTime? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var theorySession = await _context.TheorySessions
                .Include(t => t.Group)
                .Include(t => t.Instructor)
                .FirstOrDefaultAsync(m => m.StartTime == id);
            if (theorySession == null)
            {
                return NotFound();
            }

            return View(theorySession);
        }

        // GET: TheorySessions/Create
        public IActionResult Create()
        {
            ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "Id");
            ViewData["InstructorId"] = new SelectList(_context.Instructors, "Id", "Id");
            return View();
        }

        // POST: TheorySessions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StartTime,EndTime,Location,Status,InstructorId,GroupId")] TheorySession theorySession)
        {
            if (ModelState.IsValid)
            {
                _context.Add(theorySession);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "Id", theorySession.GroupId);
            ViewData["InstructorId"] = new SelectList(_context.Instructors, "Id", "Id", theorySession.InstructorId);
            return View(theorySession);
        }

        // GET: TheorySessions/Edit/5
        public async Task<IActionResult> Edit(DateTime? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var theorySession = await _context.TheorySessions.FindAsync(id);
            if (theorySession == null)
            {
                return NotFound();
            }
            ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "Id", theorySession.GroupId);
            ViewData["InstructorId"] = new SelectList(_context.Instructors, "Id", "Id", theorySession.InstructorId);
            return View(theorySession);
        }

        // POST: TheorySessions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DateTime id, [Bind("StartTime,EndTime,Location,Status,InstructorId,GroupId")] TheorySession theorySession)
        {
            if (id != theorySession.StartTime)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(theorySession);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TheorySessionExists(theorySession.StartTime))
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
            ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "Id", theorySession.GroupId);
            ViewData["InstructorId"] = new SelectList(_context.Instructors, "Id", "Id", theorySession.InstructorId);
            return View(theorySession);
        }

        // GET: TheorySessions/Delete/5
        public async Task<IActionResult> Delete(DateTime? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var theorySession = await _context.TheorySessions
                .Include(t => t.Group)
                .Include(t => t.Instructor)
                .FirstOrDefaultAsync(m => m.StartTime == id);
            if (theorySession == null)
            {
                return NotFound();
            }

            return View(theorySession);
        }

        // POST: TheorySessions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(DateTime id)
        {
            var theorySession = await _context.TheorySessions.FindAsync(id);
            if (theorySession != null)
            {
                _context.TheorySessions.Remove(theorySession);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TheorySessionExists(DateTime id)
        {
            return _context.TheorySessions.Any(e => e.StartTime == id);
        }
    }
}
