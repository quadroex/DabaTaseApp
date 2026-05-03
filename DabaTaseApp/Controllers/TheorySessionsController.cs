using DabaTaseApp.Models;
using DabaTaseApp.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DabaTaseApp.Controllers
{
    [Authorize(Roles = AppRoles.AllAuthenticated)]
    public class TheorySessionsController : Controller
    {
        private readonly Lab1Context _context;

        public TheorySessionsController(Lab1Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            await RefreshSessionStatusesAsync();

            var query = _context.TheorySessions
                .Include(t => t.Group)
                .Include(t => t.Instructor)
                .OrderBy(t => t.StartTime)
                .AsQueryable();

            if (User.IsInRole(AppRoles.Student))
            {
                var student = await GetCurrentStudentAsync();
                if (student?.GroupId == null)
                {
                    return View(Array.Empty<TheorySession>());
                }

                query = query.Where(t => t.GroupId == student.GroupId);
            }
            else if (User.IsInRole(AppRoles.Instructor) && !User.IsInRole(AppRoles.Admin))
            {
                var instructor = await GetCurrentInstructorAsync();
                if (instructor == null)
                {
                    return View(Array.Empty<TheorySession>());
                }

                query = query.Where(t => t.InstructorId == instructor.Id);
            }

            return View(await query.ToListAsync());
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

            if (User.IsInRole(AppRoles.Student))
            {
                var student = await GetCurrentStudentAsync();
                if (student?.GroupId == null || theorySession.GroupId != student.GroupId)
                {
                    return Forbid();
                }
            }
            else if (User.IsInRole(AppRoles.Instructor) && !User.IsInRole(AppRoles.Admin))
            {
                var instructor = await GetCurrentInstructorAsync();
                if (instructor == null || theorySession.InstructorId != instructor.Id)
                {
                    return Forbid();
                }
            }

            return View(theorySession);
        }

        [Authorize(Roles = AppRoles.AdminOrInstructor)]
        public async Task<IActionResult> Create()
        {
            if (User.IsInRole(AppRoles.Instructor) && !User.IsInRole(AppRoles.Admin) && await GetCurrentInstructorAsync() == null)
            {
                return Forbid();
            }

            await PopulateSelectListsAsync();
            return View();
        }

        [Authorize(Roles = AppRoles.AdminOrInstructor)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,StartTime,InstructorId,GroupId,Location,EndTime,Status")] TheorySession theorySession)
        {
            if (User.IsInRole(AppRoles.Instructor) && !User.IsInRole(AppRoles.Admin))
            {
                var instructor = await GetCurrentInstructorAsync();
                if (instructor == null)
                {
                    return Forbid();
                }

                theorySession.InstructorId = instructor.Id;
            }

            ValidateSessionWindow(theorySession);
            ModelState.Remove(nameof(TheorySession.Group));
            ModelState.Remove(nameof(TheorySession.Instructor));

            if (ModelState.IsValid)
            {
                _context.Add(theorySession);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await PopulateSelectListsAsync(theorySession);
            return View(theorySession);
        }

        [Authorize(Roles = AppRoles.AdminOrInstructor)]
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

            if (User.IsInRole(AppRoles.Instructor) && !User.IsInRole(AppRoles.Admin))
            {
                var instructor = await GetCurrentInstructorAsync();
                if (instructor == null || theorySession.InstructorId != instructor.Id)
                {
                    return Forbid();
                }
            }

            await PopulateSelectListsAsync(theorySession);
            return View(theorySession);
        }

        [Authorize(Roles = AppRoles.AdminOrInstructor)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StartTime,InstructorId,GroupId,Location,EndTime,Status")] TheorySession theorySession)
        {
            if (id != theorySession.Id)
            {
                return NotFound();
            }

            if (User.IsInRole(AppRoles.Instructor) && !User.IsInRole(AppRoles.Admin))
            {
                var instructor = await GetCurrentInstructorAsync();
                var existingSession = await _context.TheorySessions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
                if (existingSession == null)
                {
                    return NotFound();
                }

                if (instructor == null || existingSession.InstructorId != instructor.Id)
                {
                    return Forbid();
                }

                theorySession.InstructorId = instructor.Id;
            }

            ValidateSessionWindow(theorySession);
            ModelState.Remove(nameof(TheorySession.Group));
            ModelState.Remove(nameof(TheorySession.Instructor));

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

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            await PopulateSelectListsAsync(theorySession);
            return View(theorySession);
        }

        [Authorize(Roles = AppRoles.AdminOrInstructor)]
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

            if (User.IsInRole(AppRoles.Instructor) && !User.IsInRole(AppRoles.Admin))
            {
                var instructor = await GetCurrentInstructorAsync();
                if (instructor == null || theorySession.InstructorId != instructor.Id)
                {
                    return Forbid();
                }
            }

            return View(theorySession);
        }

        [Authorize(Roles = AppRoles.AdminOrInstructor)]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var theorySession = await _context.TheorySessions.FindAsync(id);
            if (theorySession != null)
            {
                if (User.IsInRole(AppRoles.Instructor) && !User.IsInRole(AppRoles.Admin))
                {
                    var instructor = await GetCurrentInstructorAsync();
                    if (instructor == null || theorySession.InstructorId != instructor.Id)
                    {
                        return Forbid();
                    }
                }

                _context.TheorySessions.Remove(theorySession);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateSelectListsAsync(TheorySession? theorySession = null)
        {
            var instructorsQuery = _context.Instructors.OrderBy(i => i.FullName).AsQueryable();
            if (User.IsInRole(AppRoles.Instructor) && !User.IsInRole(AppRoles.Admin))
            {
                var currentInstructor = await GetCurrentInstructorAsync();
                instructorsQuery = currentInstructor == null
                    ? instructorsQuery.Where(i => false)
                    : instructorsQuery.Where(i => i.Id == currentInstructor.Id);
            }

            ViewData["GroupId"] = new SelectList(
                await _context.Groups.OrderBy(g => g.GroupName).ToListAsync(),
                "Id",
                "GroupName",
                theorySession?.GroupId);

            ViewData["InstructorId"] = new SelectList(
                await instructorsQuery.ToListAsync(),
                "Id",
                "FullName",
                theorySession?.InstructorId);
        }

        private async Task RefreshSessionStatusesAsync()
        {
            var now = DateTime.Now;
            var sessionsToUpdate = await _context.TheorySessions
                .Where(s => s.Status != "Завершено" && s.Status != "Скасовано")
                .ToListAsync();

            var isUpdated = false;
            foreach (var session in sessionsToUpdate)
            {
                if (now >= session.EndTime && session.Status != "Завершено")
                {
                    session.Status = "Завершено";
                    isUpdated = true;
                }
                else if (now >= session.StartTime && now < session.EndTime && session.Status != "Триває")
                {
                    session.Status = "Триває";
                    isUpdated = true;
                }
            }

            if (isUpdated)
            {
                await _context.SaveChangesAsync();
            }
        }

        private async Task<Student?> GetCurrentStudentAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrWhiteSpace(userId)
                ? null
                : await _context.Students.FirstOrDefaultAsync(s => s.ApplicationUserId == userId);
        }

        private async Task<Instructor?> GetCurrentInstructorAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrWhiteSpace(userId)
                ? null
                : await _context.Instructors.FirstOrDefaultAsync(i => i.ApplicationUserId == userId);
        }

        private void ValidateSessionWindow(TheorySession theorySession)
        {
            if (theorySession.EndTime <= theorySession.StartTime)
            {
                ModelState.AddModelError(nameof(TheorySession.EndTime), "Час закінчення повинен бути пізніше за час початку.");
            }
        }

        private bool TheorySessionExists(int id)
        {
            return _context.TheorySessions.Any(e => e.Id == id);
        }
    }
}
