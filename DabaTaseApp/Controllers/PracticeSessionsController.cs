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
        private const string Planned = "Заплановано";
        private const string Ongoing = "Триває";
        private const string Finished = "Завершено";
        private const string Cancelled = "Скасовано";

        private static readonly string[] AllowedStatuses = [Planned, Ongoing, Finished, Cancelled];

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
                    TempData["ErrorMessage"] = "Ваш акаунт ще не прив'язаний до картки учня.";
                    return View(Array.Empty<PracticeSession>());
                }

                query = query.Where(p => p.StudentId == student.Id);
            }
            else if (User.IsInRole(AppRoles.Instructor) && !User.IsInRole(AppRoles.Admin))
            {
                var instructor = await GetCurrentInstructorAsync();
                if (instructor == null)
                {
                    TempData["ErrorMessage"] = "Ваш акаунт ще не прив'язаний до картки інструктора.";
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

            if (!await UserCanAccessPracticeSessionAsync(practiceSession))
            {
                return Forbid();
            }

            return View(practiceSession);
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
            return View(new PracticeSession
            {
                StartTime = DateTime.Now.Date.AddHours(10),
                EndTime = DateTime.Now.Date.AddHours(11),
                Status = Planned
            });
        }

        [Authorize(Roles = AppRoles.AdminOrInstructor)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StudentId,InstructorId,VehiclePlate,StartTime,EndTime")] PracticeSession practiceSession)
        {
            RemoveNavigationModelState();
            await ApplyInstructorScopeAsync(practiceSession);
            await ValidateSessionAsync(practiceSession);

            if (ModelState.IsValid)
            {
                practiceSession.Status = ComputeStatus(practiceSession.StartTime, practiceSession.EndTime);
                _context.Add(practiceSession);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Практичне заняття створено.";
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

            var practiceSession = await _context.PracticeSessions
                .Include(p => p.Student)
                .Include(p => p.Instructor)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (practiceSession == null)
            {
                return NotFound();
            }

            if (!await UserCanManagePracticeSessionAsync(practiceSession))
            {
                return Forbid();
            }

            await PopulateSelectListsAsync(practiceSession);
            return View(practiceSession);
        }

        [Authorize(Roles = AppRoles.AdminOrInstructor)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StartTime,EndTime,VehiclePlate")] PracticeSession formData)
        {
            var existing = await _context.PracticeSessions.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            if (!await UserCanManagePracticeSessionAsync(existing))
            {
                return Forbid();
            }

            existing.StartTime = formData.StartTime;
            existing.EndTime = formData.EndTime;
            existing.VehiclePlate = (formData.VehiclePlate ?? string.Empty).Trim().ToUpperInvariant();

            RemoveNavigationModelState();
            ModelState.Remove(nameof(PracticeSession.Id));

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
                    if (!PracticeSessionExists(id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                TempData["SuccessMessage"] = "Практичне заняття оновлено.";
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
            var practiceSession = await _context.PracticeSessions.FindAsync(id);
            if (practiceSession == null)
            {
                return NotFound();
            }

            if (!await UserCanManagePracticeSessionAsync(practiceSession))
            {
                return Forbid();
            }

            practiceSession.Status = Cancelled;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Практичне заняття скасовано.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = AppRoles.Admin)]
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

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var practiceSession = await _context.PracticeSessions.FindAsync(id);
            if (practiceSession != null)
            {
                _context.PracticeSessions.Remove(practiceSession);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Практичне заняття видалено.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateSelectListsAsync(PracticeSession? practiceSession = null)
        {
            var instructorsQuery = _context.Instructors.OrderBy(i => i.FullName).AsQueryable();
            ViewBag.LockInstructor = User.IsInRole(AppRoles.Instructor) && !User.IsInRole(AppRoles.Admin);

            if (ViewBag.LockInstructor == true)
            {
                var instructor = await GetCurrentInstructorAsync();
                var instructorId = instructor?.Id ?? 0;
                instructorsQuery = instructorsQuery.Where(i => i.Id == instructorId);
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

            var selectedVehicle = practiceSession?.VehiclePlate;
            var vehiclesQuery = _context.Vehicles.AsQueryable();
            vehiclesQuery = string.IsNullOrWhiteSpace(selectedVehicle)
                ? vehiclesQuery.Where(v => v.IsActive)
                : vehiclesQuery.Where(v => v.IsActive || v.PlateNumber == selectedVehicle);

            ViewData["VehiclePlate"] = new SelectList(
                await vehiclesQuery.OrderBy(v => v.PlateNumber).ToListAsync(),
                "PlateNumber",
                "PlateNumber",
                selectedVehicle);
        }

        private async Task RefreshSessionStatusesAsync()
        {
            var now = DateTime.Now;
            var sessionsToUpdate = await _context.PracticeSessions
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

        private async Task ApplyInstructorScopeAsync(PracticeSession practiceSession)
        {
            practiceSession.VehiclePlate = (practiceSession.VehiclePlate ?? string.Empty).Trim().ToUpperInvariant();

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

            practiceSession.InstructorId = instructor.Id;
        }

        private async Task ValidateSessionAsync(PracticeSession practiceSession)
        {
            if (practiceSession.EndTime <= practiceSession.StartTime)
            {
                ModelState.AddModelError(nameof(PracticeSession.EndTime), "Час закінчення повинен бути пізніше за час початку.");
                return;
            }

            if (!await _context.Students.AnyAsync(s => s.Id == practiceSession.StudentId))
            {
                ModelState.AddModelError(nameof(PracticeSession.StudentId), "Оберіть існуючого учня.");
            }

            if (!await _context.Instructors.AnyAsync(i => i.Id == practiceSession.InstructorId))
            {
                ModelState.AddModelError(nameof(PracticeSession.InstructorId), "Оберіть існуючого інструктора.");
            }

            if (!await _context.Vehicles.AnyAsync(v => v.PlateNumber == practiceSession.VehiclePlate && v.IsActive))
            {
                ModelState.AddModelError(nameof(PracticeSession.VehiclePlate), "Не можна призначити неактивний автомобіль.");
            }

            var hasStudentConflict = await _context.PracticeSessions.AnyAsync(s =>
                s.Id != practiceSession.Id
                && s.Status != Cancelled
                && s.StudentId == practiceSession.StudentId
                && s.StartTime < practiceSession.EndTime
                && practiceSession.StartTime < s.EndTime);
            if (hasStudentConflict)
            {
                ModelState.AddModelError(nameof(PracticeSession.StudentId), "Учень уже має практичне заняття в цей час.");
            }

            var hasVehicleConflict = await _context.PracticeSessions.AnyAsync(s =>
                s.Id != practiceSession.Id
                && s.Status != Cancelled
                && s.VehiclePlate == practiceSession.VehiclePlate
                && s.StartTime < practiceSession.EndTime
                && practiceSession.StartTime < s.EndTime);
            if (hasVehicleConflict)
            {
                ModelState.AddModelError(nameof(PracticeSession.VehiclePlate), "Автомобіль уже зайнятий у цей час.");
            }

            var hasPracticeInstructorConflict = await _context.PracticeSessions.AnyAsync(s =>
                s.Id != practiceSession.Id
                && s.Status != Cancelled
                && s.InstructorId == practiceSession.InstructorId
                && s.StartTime < practiceSession.EndTime
                && practiceSession.StartTime < s.EndTime);

            var hasTheoryInstructorConflict = await _context.TheorySessions.AnyAsync(s =>
                s.Status != Cancelled
                && s.InstructorId == practiceSession.InstructorId
                && s.StartTime < practiceSession.EndTime
                && practiceSession.StartTime < s.EndTime);

            if (hasPracticeInstructorConflict || hasTheoryInstructorConflict)
            {
                ModelState.AddModelError(nameof(PracticeSession.InstructorId), "Інструктор уже має заняття в цей час.");
            }
        }

        private async Task<bool> UserCanAccessPracticeSessionAsync(PracticeSession practiceSession)
        {
            if (User.IsInRole(AppRoles.Admin))
            {
                return true;
            }

            if (User.IsInRole(AppRoles.Student))
            {
                var student = await GetCurrentStudentAsync();
                return student != null && practiceSession.StudentId == student.Id;
            }

            return await UserCanManagePracticeSessionAsync(practiceSession);
        }

        private async Task<bool> UserCanManagePracticeSessionAsync(PracticeSession practiceSession)
        {
            if (User.IsInRole(AppRoles.Admin))
            {
                return true;
            }

            var instructor = await GetCurrentInstructorAsync();
            return instructor != null && practiceSession.InstructorId == instructor.Id;
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
