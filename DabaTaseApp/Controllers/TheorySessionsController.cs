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

        public async Task<IActionResult> Index()
        {
            var lab1Context = _context.TheorySessions.Include(t => t.Group).Include(t => t.Instructor);
            return View(await lab1Context.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var theorySession = await _context.TheorySessions
                .Include(t => t.Group)
                .Include(t => t.Instructor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (theorySession == null)
            {
                return NotFound();
            }

            return View(theorySession);
        }

        public IActionResult Create()
        {
            ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "GroupName");
            ViewData["InstructorId"] = new SelectList(_context.Instructors, "Id", "FullName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,StartTime,InstructorId,GroupId,Location,EndTime,Status")] TheorySession theorySession)
        {
            if (theorySession.EndTime <= theorySession.StartTime)
            {
                ModelState.AddModelError("EndTime", "Час закінчення повинен бути пізніше за час початку.");
            }

            ModelState.Remove("Group");
            ModelState.Remove("Instructor");

            if (ModelState.IsValid)
            {
                _context.Add(theorySession);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "GroupName", theorySession.GroupId);
            ViewData["InstructorId"] = new SelectList(_context.Instructors, "Id", "FullName", theorySession.InstructorId);
            return View(theorySession);
        }

        public async Task<IActionResult> Edit(int? id)
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
            ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "GroupName", theorySession.GroupId);
            ViewData["InstructorId"] = new SelectList(_context.Instructors, "Id", "FullName", theorySession.InstructorId);
            return View(theorySession);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StartTime,InstructorId,GroupId,Location,EndTime,Status")] TheorySession theorySession)
        {
            if (id != theorySession.Id)
            {
                return NotFound();
            }

            if (theorySession.EndTime <= theorySession.StartTime)
            {
                ModelState.AddModelError("EndTime", "Час закінчення повинен бути пізніше за час початку.");
            }

            ModelState.Remove("Group");
            ModelState.Remove("Instructor");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(theorySession);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TheorySessionExists(theorySession.Id))
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
            ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "GroupName", theorySession.GroupId);
            ViewData["InstructorId"] = new SelectList(_context.Instructors, "Id", "FullName", theorySession.InstructorId);
            return View(theorySession);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var theorySession = await _context.TheorySessions
                .Include(t => t.Group)
                .Include(t => t.Instructor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (theorySession == null)
            {
                return NotFound();
            }

            return View(theorySession);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var theorySession = await _context.TheorySessions.FindAsync(id);
            if (theorySession != null)
            {
                _context.TheorySessions.Remove(theorySession);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TheorySessionExists(int id)
        {
            return _context.TheorySessions.Any(e => e.Id == id);
        }
    }
}