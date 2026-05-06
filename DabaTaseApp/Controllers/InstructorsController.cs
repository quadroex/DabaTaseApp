using DabaTaseApp.Models;
using DabaTaseApp.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DabaTaseApp.Controllers
{
    [Authorize(Roles = AppRoles.AdminOrInstructor)]
    public class InstructorsController : Controller
    {
        private readonly Lab1Context _context;
        private readonly UserManager<IdentityUser> _userManager;

        public InstructorsController(Lab1Context context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var query = _context.Instructors
                .Include(i => i.ApplicationUser)
                .OrderBy(i => i.FullName)
                .AsQueryable();

            if (User.IsInRole(AppRoles.Instructor) && !User.IsInRole(AppRoles.Admin))
            {
                var instructor = await GetCurrentInstructorAsync();
                if (instructor == null)
                {
                    TempData["ErrorMessage"] = "Ваш акаунт ще не прив'язаний до картки інструктора.";
                    return View(Array.Empty<Instructor>());
                }

                query = query.Where(i => i.Id == instructor.Id);
            }

            return View(await query.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var instructor = await _context.Instructors
                .Include(i => i.ApplicationUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (instructor == null)
            {
                return NotFound();
            }

            if (!await UserCanAccessInstructorAsync(instructor))
            {
                return Forbid();
            }

            return View(instructor);
        }

        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Create()
        {
            await PopulateAccountSelectAsync();
            return View();
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ApplicationUserId,FullName,PhoneNumber,LicenseSerial")] Instructor instructor)
        {
            await ValidateInstructorAsync(instructor);
            RemoveNavigationModelState();

            if (ModelState.IsValid)
            {
                _context.Add(instructor);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Інструктора створено.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateAccountSelectAsync(instructor.ApplicationUserId, instructor.Id);
            return View(instructor);
        }

        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var instructor = await _context.Instructors.FindAsync(id);
            if (instructor == null)
            {
                return NotFound();
            }

            await PopulateAccountSelectAsync(instructor.ApplicationUserId, instructor.Id);
            return View(instructor);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ApplicationUserId,FullName,PhoneNumber,LicenseSerial")] Instructor instructor)
        {
            if (id != instructor.Id)
            {
                return NotFound();
            }

            await ValidateInstructorAsync(instructor);
            RemoveNavigationModelState();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(instructor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InstructorExists(instructor.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                TempData["SuccessMessage"] = "Картку інструктора оновлено.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateAccountSelectAsync(instructor.ApplicationUserId, instructor.Id);
            return View(instructor);
        }

        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var instructor = await _context.Instructors
                .Include(i => i.ApplicationUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (instructor == null)
            {
                return NotFound();
            }

            return View(instructor);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var instructor = await _context.Instructors.FindAsync(id);
            if (instructor == null)
            {
                return RedirectToAction(nameof(Index));
            }

            if (await _context.Groups.AnyAsync(g => g.TheoryInstructorId == id)
                || await _context.TheorySessions.AnyAsync(s => s.InstructorId == id)
                || await _context.PracticeSessions.AnyAsync(s => s.InstructorId == id)
                || await _context.Instructors.AnyAsync(i => i.Id == id && i.CategoryNames.Any()))
            {
                TempData["ErrorMessage"] = "Неможливо видалити інструктора, оскільки за ним закріплені групи, заняття або категорії.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.Instructors.Remove(instructor);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Інструктора видалено.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateAccountSelectAsync(string? selectedUserId = null, int instructorId = 0)
        {
            var instructorUsers = await _userManager.GetUsersInRoleAsync(AppRoles.Instructor);
            var linkedUserIds = await _context.Instructors
                .Where(i => i.Id != instructorId && i.ApplicationUserId != null)
                .Select(i => i.ApplicationUserId!)
                .ToListAsync();

            var availableUsers = instructorUsers
                .Where(u => !linkedUserIds.Contains(u.Id) || u.Id == selectedUserId)
                .OrderBy(u => u.Email)
                .ToList();

            ViewData["ApplicationUserId"] = new SelectList(availableUsers, "Id", "Email", selectedUserId);
        }

        private async Task<Instructor?> GetCurrentInstructorAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrWhiteSpace(userId)
                ? null
                : await _context.Instructors.FirstOrDefaultAsync(i => i.ApplicationUserId == userId);
        }

        private async Task<bool> UserCanAccessInstructorAsync(Instructor instructor)
        {
            if (User.IsInRole(AppRoles.Admin))
            {
                return true;
            }

            var currentInstructor = await GetCurrentInstructorAsync();
            return currentInstructor != null && currentInstructor.Id == instructor.Id;
        }

        private bool InstructorExists(int id)
        {
            return _context.Instructors.Any(e => e.Id == id);
        }

        private async Task ValidateInstructorAsync(Instructor instructor)
        {
            instructor.ApplicationUserId = string.IsNullOrWhiteSpace(instructor.ApplicationUserId)
                ? null
                : instructor.ApplicationUserId.Trim();
            instructor.FullName = (instructor.FullName ?? string.Empty).Trim();
            instructor.PhoneNumber = (instructor.PhoneNumber ?? string.Empty).Trim();
            instructor.LicenseSerial = (instructor.LicenseSerial ?? string.Empty).Trim().ToUpperInvariant();

            if (await _context.Instructors.AnyAsync(i => i.Id != instructor.Id && i.FullName == instructor.FullName))
            {
                ModelState.AddModelError(nameof(Instructor.FullName), "Інструктор із таким ПІБ вже існує.");
            }

            if (await _context.Instructors.AnyAsync(i => i.Id != instructor.Id && i.LicenseSerial == instructor.LicenseSerial))
            {
                ModelState.AddModelError(nameof(Instructor.LicenseSerial), "Інструктор із такою ліцензією вже існує.");
            }

            if (await _context.Instructors.AnyAsync(i => i.Id != instructor.Id && i.PhoneNumber == instructor.PhoneNumber))
            {
                ModelState.AddModelError(nameof(Instructor.PhoneNumber), "Інструктор із таким телефоном вже існує.");
            }

            await ValidateApplicationUserLinkAsync(instructor);
        }

        private async Task ValidateApplicationUserLinkAsync(Instructor instructor)
        {
            if (string.IsNullOrWhiteSpace(instructor.ApplicationUserId))
            {
                instructor.ApplicationUserId = null;
                return;
            }

            var user = await _userManager.FindByIdAsync(instructor.ApplicationUserId);
            if (user == null)
            {
                ModelState.AddModelError(nameof(Instructor.ApplicationUserId), "Обраний акаунт не існує.");
                return;
            }

            if (!await _userManager.IsInRoleAsync(user, AppRoles.Instructor))
            {
                ModelState.AddModelError(nameof(Instructor.ApplicationUserId), "До картки інструктора можна прив'язати тільки акаунт із роллю instructor.");
                return;
            }

            var alreadyLinked = await _context.Instructors
                .AnyAsync(i => i.Id != instructor.Id && i.ApplicationUserId == instructor.ApplicationUserId);

            if (alreadyLinked)
            {
                ModelState.AddModelError(nameof(Instructor.ApplicationUserId), "Цей акаунт уже прив'язаний до іншого інструктора.");
            }
        }

        private void RemoveNavigationModelState()
        {
            ModelState.Remove(nameof(Instructor.ApplicationUser));
            ModelState.Remove(nameof(Instructor.Groups));
            ModelState.Remove(nameof(Instructor.PracticeSessions));
            ModelState.Remove(nameof(Instructor.TheorySessions));
            ModelState.Remove(nameof(Instructor.CategoryNames));
        }
    }
}
