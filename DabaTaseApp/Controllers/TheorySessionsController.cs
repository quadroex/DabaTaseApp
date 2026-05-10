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
        private const string Planned = "Заплановано";
        private const string Ongoing = "Триває";
        private const string Finished = "Завершено";
        private const string Cancelled = "Скасовано";

        private static readonly string[] AllowedStatuses = [Planned, Ongoing, Finished, Cancelled];

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
                    TempData["ErrorMessage"] = "Ваш акаунт ще не прив'язаний до картки учня або учень не має групи.";
                    return View(Array.Empty<TheorySession>());
                }

                query = query.Where(t => t.GroupId == student.GroupId);
            }
            else if (User.IsInRole(AppRoles.Instructor) && !User.IsInRole(AppRoles.Admin))
            {
                var instructor = await GetCurrentInstructorAsync();
                if (instructor == null)
                {
                    TempData["ErrorMessage"] = "Ваш акаунт ще не прив'язаний до картки інструктора.";
                    return View(Array.Empty<TheorySession>());
                }

                query = query.Where(t => t.Group.TheoryInstructorId == instructor.Id);
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

            if (!await UserCanAccessTheorySessionAsync(theorySession))
            {
                return Forbid();
            }

            return View(theorySession);
        }

        [Authorize(Roles = AppRoles.AdminOrInstructor)]
        public async Task<IActionResult> Create()
        {
            if (User.IsInRole(AppRoles.Instructor) && !User.IsInRole(AppRoles.Admin)
                && await GetCurrentInstructorAsync() == null)
            {
                TempData["ErrorMessage"] = "Ваш акаунт ще не прив'язаний до картки інструктора.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateSelectListsAsync();
            return View(new TheorySession
            {
                StartTime = DateTime.Now.Date.AddHours(9),
                EndTime = DateTime.Now.Date.AddHours(10),
                Status = Planned
            });
        }

        [Authorize(Roles = AppRoles.AdminOrInstructor)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StartTime,InstructorId,GroupId,Location,EndTime")] TheorySession theorySession)
        {
            RemoveNavigationModelState();
            await ApplyInstructorScopeAsync(theorySession);
            await ValidateSessionAsync(theorySession);

            if (ModelState.IsValid)
            {
                theorySession.Status = ComputeStatus(theorySession.StartTime, theorySession.EndTime);
                _context.Add(theorySession);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Теоретичне заняття створено.";
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

            var theorySession = await _context.TheorySessions
                .Include(t => t.Group)
                .Include(t => t.Instructor)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (theorySession == null)
            {
                return NotFound();
            }

            if (!await UserCanManageTheorySessionAsync(theorySession))
            {
                return Forbid();
            }

            await PopulateSelectListsAsync(theorySession);
            return View(theorySession);
        }

        [Authorize(Roles = AppRoles.AdminOrInstructor)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StartTime,EndTime,Location")] TheorySession formData)
        {
            var existing = await _context.TheorySessions
                .Include(t => t.Group)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (existing == null)
            {
                return NotFound();
            }

            if (!await UserCanManageTheorySessionAsync(existing))
            {
                return Forbid();
            }

            existing.StartTime = formData.StartTime;
            existing.EndTime = formData.EndTime;
            existing.Location = (formData.Location ?? string.Empty).Trim();

            RemoveNavigationModelState();
            ModelState.Remove(nameof(TheorySession.Id));

            await ValidateSessionAsync(existing);

            if (ModelState.IsValid)
            {
                if (existing.Status != Cancelled)
                {
                    existing.Status = ComputeStatus(existing.StartTime, existing.EndTime);
                }

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TheorySessionExists(id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                TempData["SuccessMessage"] = "Теоретичне заняття оновлено.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateSelectListsAsync(existing);
            return View(existing);
        }

        [Authorize(Roles = AppRoles.AdminOrInstructor)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var theorySession = await _context.TheorySessions
                .Include(t => t.Group)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (theorySession == null)
            {
                return NotFound();
            }

            if (!await UserCanManageTheorySessionAsync(theorySession))
            {
                return Forbid();
            }

            theorySession.Status = Cancelled;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Теоретичне заняття скасовано.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = AppRoles.Admin)]
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

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var theorySession = await _context.TheorySessions.FindAsync(id);
            if (theorySession != null)
            {
                _context.TheorySessions.Remove(theorySession);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Теоретичне заняття видалено.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateSelectListsAsync(TheorySession? theorySession = null)
        {
            var groupsQuery = _context.Groups.OrderBy(g => g.GroupName).AsQueryable();
            var instructorsQuery = _context.Instructors.OrderBy(i => i.FullName).AsQueryable();
            ViewBag.LockInstructor = User.IsInRole(AppRoles.Instructor) && !User.IsInRole(AppRoles.Admin);

            if (ViewBag.LockInstructor == true)
            {
                var instructor = await GetCurrentInstructorAsync();
                var instructorId = instructor?.Id ?? 0;
                groupsQuery = groupsQuery.Where(g => g.TheoryInstructorId == instructorId);
                instructorsQuery = instructorsQuery.Where(i => i.Id == instructorId);
            }

            ViewData["GroupId"] = new SelectList(
                await groupsQuery.ToListAsync(),
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
            var sessionsToUpdate = await _context.TheorySessions
                .Where(s => s.Status != Cancelled)
                .ToListAsync();

            var isUpdated = false;
            foreach (var session in sessionsToUpdate)
            {
                var expected = ComputeStatus(session.StartTime, session.EndTime);
                if (session.Status != expected)
                {
                    session.Status = expected;
                    isUpdated = true;
                }
            }

            if (isUpdated)
            {
                await _context.SaveChangesAsync();
            }
        }

        private static string ComputeStatus(DateTime start, DateTime end)
        {
            var now = DateTime.Now;
            if (now >= end) return Finished;
            if (now >= start) return Ongoing;
            return Planned;
        }

        private async Task ApplyInstructorScopeAsync(TheorySession theorySession)
        {
            theorySession.Location = (theorySession.Location ?? string.Empty).Trim();

            if (User.IsInRole(AppRoles.Admin))
            {
                return;
            }

            var instructor = await GetCurrentInstructorAsync();
            if (instructor == null)
            {
                ModelState.AddModelError(string.Empty, "Ваш акаунт ще не прив'язаний до картки інструктора.");
                return;
            }

            theorySession.InstructorId = instructor.Id;

            var ownsGroup = await _context.Groups
                .AnyAsync(g => g.Id == theorySession.GroupId && g.TheoryInstructorId == instructor.Id);
            if (!ownsGroup)
            {
                ModelState.AddModelError(nameof(TheorySession.GroupId), "Інструктор може планувати теорію лише для своїх груп.");
            }
        }

        private async Task ValidateSessionAsync(TheorySession theorySession)
        {
            if (theorySession.EndTime <= theorySession.StartTime)
            {
                ModelState.AddModelError(nameof(TheorySession.EndTime), "Час закінчення повинен бути пізніше за час початку.");
                return;
            }

            var group = await _context.Groups.FindAsync(theorySession.GroupId);
            if (group == null)
            {
                ModelState.AddModelError(nameof(TheorySession.GroupId), "Оберіть існуючу групу.");
                return;
            }

            if (!await _context.Instructors.AnyAsync(i => i.Id == theorySession.InstructorId))
            {
                ModelState.AddModelError(nameof(TheorySession.InstructorId), "Оберіть існуючого інструктора.");
                return;
            }

            if (group.TheoryInstructorId.HasValue && group.TheoryInstructorId.Value != theorySession.InstructorId)
            {
                ModelState.AddModelError(nameof(TheorySession.InstructorId), "Теоретичне заняття має вести інструктор, призначений у групі.");
            }

            var hasGroupConflict = await _context.TheorySessions.AnyAsync(s =>
                s.Id != theorySession.Id
                && s.Status != Cancelled
                && s.GroupId == theorySession.GroupId
                && s.StartTime < theorySession.EndTime
                && theorySession.StartTime < s.EndTime);
            if (hasGroupConflict)
            {
                ModelState.AddModelError(nameof(TheorySession.GroupId), "У групи вже є теоретичне заняття в цей час.");
            }

            var hasTheoryInstructorConflict = await _context.TheorySessions.AnyAsync(s =>
                s.Id != theorySession.Id
                && s.Status != Cancelled
                && s.InstructorId == theorySession.InstructorId
                && s.StartTime < theorySession.EndTime
                && theorySession.StartTime < s.EndTime);

            var hasPracticeInstructorConflict = await _context.PracticeSessions.AnyAsync(s =>
                s.Status != Cancelled
                && s.InstructorId == theorySession.InstructorId
                && s.StartTime < theorySession.EndTime
                && theorySession.StartTime < s.EndTime);

            if (hasTheoryInstructorConflict || hasPracticeInstructorConflict)
            {
                ModelState.AddModelError(nameof(TheorySession.InstructorId), "Інструктор уже має заняття в цей час.");
            }
        }

        private async Task<bool> UserCanAccessTheorySessionAsync(TheorySession theorySession)
        {
            if (User.IsInRole(AppRoles.Admin))
            {
                return true;
            }

            if (User.IsInRole(AppRoles.Student))
            {
                var student = await GetCurrentStudentAsync();
                return student?.GroupId != null && theorySession.GroupId == student.GroupId;
            }

            return await UserCanManageTheorySessionAsync(theorySession);
        }

        private async Task<bool> UserCanManageTheorySessionAsync(TheorySession theorySession)
        {
            if (User.IsInRole(AppRoles.Admin))
            {
                return true;
            }

            var instructor = await GetCurrentInstructorAsync();
            return instructor != null && theorySession.Group?.TheoryInstructorId == instructor.Id;
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

        private void RemoveNavigationModelState()
        {
            ModelState.Remove(nameof(TheorySession.Group));
            ModelState.Remove(nameof(TheorySession.Instructor));
        }

        private bool TheorySessionExists(int id)
        {
            return _context.TheorySessions.Any(e => e.Id == id);
        }
    }
}
