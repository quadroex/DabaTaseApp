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
    public class PracticeSessionsController : Controller
    {
        private readonly Lab1Context _context;

        public PracticeSessionsController(Lab1Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            await RefreshSessionStatusesAsync();

            var query = _context.PracticeSessions
                .Include(p => p.Instructor)
                .Include(p => p.Student)
                .Include(p => p.VehiclePlateNavigation)
                .OrderBy(p => p.StartTime)
                .AsQueryable();

            if (User.IsInRole(AppRoles.Student))
            {
                var student = await GetCurrentStudentAsync();
                if (student == null)
                {
                    return View(Array.Empty<PracticeSession>());
                }

                query = query.Where(p => p.StudentId == student.Id);
            }
            else if (User.IsInRole(AppRoles.Instructor) && !User.IsInRole(AppRoles.Admin))
            {
                var instructor = await GetCurrentInstructorAsync();
                if (instructor == null)
                {
                    return View(Array.Empty<PracticeSession>());
                }

                query = query.Where(p => p.InstructorId == instructor.Id);
            }

            return View(await query.ToListAsync());
        }

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

            if (User.IsInRole(AppRoles.Student))
            {
                var student = await GetCurrentStudentAsync();
                if (student == null || practiceSession.StudentId != student.Id)
                {
                    return Forbid();
                }
            }
            else if (User.IsInRole(AppRoles.Instructor) && !User.IsInRole(AppRoles.Admin))
            {
                var instructor = await GetCurrentInstructorAsync();
                if (instructor == null || practiceSession.InstructorId != instructor.Id)
                {
                    return Forbid();
                }
            }

            return View(practiceSession);
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
        public async Task<IActionResult> Create([Bind("Id,StudentId,InstructorId,VehiclePlate,StartTime,EndTime,Status")] PracticeSession practiceSession)
        {
            if (User.IsInRole(AppRoles.Instructor) && !User.IsInRole(AppRoles.Admin))
            {
                var instructor = await GetCurrentInstructorAsync();
                if (instructor == null)
                {
                    return Forbid();
                }

                practiceSession.InstructorId = instructor.Id;
            }

            ValidateSessionWindow(practiceSession);
            RemoveNavigationModelState();

            if (ModelState.IsValid)
            {
                _context.Add(practiceSession);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await PopulateSelectListsAsync(practiceSession);
            return View(practiceSession);
        }

        [Authorize(Roles = AppRoles.AdminOrInstructor)]
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

            if (User.IsInRole(AppRoles.Instructor) && !User.IsInRole(AppRoles.Admin))
            {
                var instructor = await GetCurrentInstructorAsync();
                if (instructor == null || practiceSession.InstructorId != instructor.Id)
                {
                    return Forbid();
                }
            }

            await PopulateSelectListsAsync(practiceSession);
            return View(practiceSession);
        }

        [Authorize(Roles = AppRoles.AdminOrInstructor)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StudentId,InstructorId,VehiclePlate,StartTime,EndTime,Status")] PracticeSession practiceSession)
        {
            if (id != practiceSession.Id)
            {
                return NotFound();
            }

            if (User.IsInRole(AppRoles.Instructor) && !User.IsInRole(AppRoles.Admin))
            {
                var instructor = await GetCurrentInstructorAsync();
                var existingSession = await _context.PracticeSessions.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                if (existingSession == null)
                {
                    return NotFound();
                }

                if (instructor == null || existingSession.InstructorId != instructor.Id)
                {
                    return Forbid();
                }

                practiceSession.InstructorId = instructor.Id;
            }

            ValidateSessionWindow(practiceSession);
            RemoveNavigationModelState();

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

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            await PopulateSelectListsAsync(practiceSession);
            return View(practiceSession);
        }

        [Authorize(Roles = AppRoles.AdminOrInstructor)]
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

            if (User.IsInRole(AppRoles.Instructor) && !User.IsInRole(AppRoles.Admin))
            {
                var instructor = await GetCurrentInstructorAsync();
                if (instructor == null || practiceSession.InstructorId != instructor.Id)
                {
                    return Forbid();
                }
            }

            return View(practiceSession);
        }

        [Authorize(Roles = AppRoles.AdminOrInstructor)]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var practiceSession = await _context.PracticeSessions.FindAsync(id);
            if (practiceSession != null)
            {
                if (User.IsInRole(AppRoles.Instructor) && !User.IsInRole(AppRoles.Admin))
                {
                    var instructor = await GetCurrentInstructorAsync();
                    if (instructor == null || practiceSession.InstructorId != instructor.Id)
                    {
                        return Forbid();
                    }
                }

                _context.PracticeSessions.Remove(practiceSession);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateSelectListsAsync(PracticeSession? practiceSession = null)
        {
            var instructorsQuery = _context.Instructors.OrderBy(i => i.FullName).AsQueryable();
            if (User.IsInRole(AppRoles.Instructor) && !User.IsInRole(AppRoles.Admin))
            {
                var currentInstructor = await GetCurrentInstructorAsync();
                instructorsQuery = currentInstructor == null
                    ? instructorsQuery.Where(i => false)
                    : instructorsQuery.Where(i => i.Id == currentInstructor.Id);
            }

            ViewData["InstructorId"] = new SelectList(
                await instructorsQuery.ToListAsync(),
                "Id",
                "FullName",
                practiceSession?.InstructorId);

            ViewData["StudentId"] = new SelectList(
                await _context.Students.OrderBy(s => s.FullName).ToListAsync(),
                "Id",
                "FullName",
                practiceSession?.StudentId);

            ViewData["VehiclePlate"] = new SelectList(
                await _context.Vehicles.OrderBy(v => v.PlateNumber).ToListAsync(),
                "PlateNumber",
                "PlateNumber",
                practiceSession?.VehiclePlate);
        }

        private async Task RefreshSessionStatusesAsync()
        {
            var now = DateTime.Now;
            var sessionsToUpdate = await _context.PracticeSessions
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

        private void ValidateSessionWindow(PracticeSession practiceSession)
        {
            if (practiceSession.EndTime <= practiceSession.StartTime)
            {
                ModelState.AddModelError(nameof(PracticeSession.EndTime), "Час закінчення повинен бути пізніше за час початку.");
            }
        }

        private void RemoveNavigationModelState()
        {
            ModelState.Remove(nameof(PracticeSession.Instructor));
            ModelState.Remove(nameof(PracticeSession.Student));
            ModelState.Remove(nameof(PracticeSession.VehiclePlateNavigation));
        }

        private bool PracticeSessionExists(int id)
        {
            return _context.PracticeSessions.Any(e => e.Id == id);
        }
    }
}
