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
        public async Task<IActionResult> Details(DateTime? startTime, int? instructorId)
        {
            if (startTime == null || instructorId == null)
            {
                return NotFound();
            }

            var theorySession = await _context.TheorySessions
                .Include(t => t.Group)
                .Include(t => t.Instructor)
                .FirstOrDefaultAsync(m => m.StartTime == startTime && m.InstructorId == instructorId);

            if (theorySession == null)
            {
                return NotFound();
            }

            return View(theorySession);
        }

        // GET: TheorySessions/Create
        public IActionResult Create()
        {
            ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "GroupName");
            ViewData["InstructorId"] = new SelectList(_context.Instructors, "Id", "FullName");
            return View();
        }

        // POST: TheorySessions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create([Bind("StartTime,InstructorId,GroupId,Location,EndTime,Status")] TheorySession theorySession)

        {

            if (theorySession.EndTime <= theorySession.StartTime)

            {

                ModelState.AddModelError("EndTime", "Час закінченя повинен бути пізніше за час початку.");

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
        
        

        // GET: TheorySessions/Edit/5
        public async Task<IActionResult> Edit(DateTime? startTime, int? instructorId)
        {
            if (startTime == null || instructorId == null)
            {
                return NotFound();
            }

            var theorySession = await _context.TheorySessions.FindAsync(startTime, instructorId);

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

        public async Task<IActionResult> Edit(DateTime startTime, int instructorId, [Bind("StartTime,InstructorId,GroupId,Location,EndTime,Status")] TheorySession theorySession)

        {

            if (startTime != theorySession.StartTime || instructorId != theorySession.InstructorId)

            {

                return NotFound();

            }



            if (theorySession.EndTime <= theorySession.StartTime)

            {

                ModelState.AddModelError("EndTime", "Час закінченя повинен бути пізніше за час початку.");

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

                    if (!TheorySessionExists(theorySession.StartTime, theorySession.InstructorId))

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

        public async Task<IActionResult> Delete(DateTime? startTime, int? instructorId)
        {
            if (startTime == null || instructorId == null)
            {
                return NotFound();
            }

            var theorySession = await _context.TheorySessions
                .Include(t => t.Group)
                .Include(t => t.Instructor)
                .FirstOrDefaultAsync(m => m.StartTime == startTime && m.InstructorId == instructorId);

            if (theorySession == null)
            {
                return NotFound();
            }

            return View(theorySession);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(DateTime startTime, int instructorId)
        {
            var theorySession = await _context.TheorySessions.FindAsync(startTime, instructorId);

            if (theorySession != null)
            {
                _context.TheorySessions.Remove(theorySession);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TheorySessionExists(DateTime startTime, int instructorId)
        {
            return _context.TheorySessions.Any(e => e.StartTime == startTime && e.InstructorId == instructorId);
        }
    }
}
