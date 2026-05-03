using DabaTaseApp.Models;
using DabaTaseApp.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DabaTaseApp.Controllers
{
    [Authorize(Roles = AppRoles.AdminOrInstructor)]
    public class InstructorsController : Controller
    {
        private readonly Lab1Context _context;

        public InstructorsController(Lab1Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var instructors = await _context.Instructors
                .OrderBy(i => i.FullName)
                .ToListAsync();

            return View(instructors);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var instructor = await _context.Instructors.FirstOrDefaultAsync(m => m.Id == id);
            if (instructor == null)
            {
                return NotFound();
            }

            return View(instructor);
        }

        [Authorize(Roles = AppRoles.Admin)]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FullName,PhoneNumber,LicenseSerial")] Instructor instructor)
        {
            await ValidateInstructorAsync(instructor);
            RemoveNavigationModelState();

            if (ModelState.IsValid)
            {
                _context.Add(instructor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

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

            return View(instructor);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,PhoneNumber,LicenseSerial")] Instructor instructor)
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

                return RedirectToAction(nameof(Index));
            }

            return View(instructor);
        }

        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var instructor = await _context.Instructors.FirstOrDefaultAsync(m => m.Id == id);
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
            if (instructor != null)
            {
                try
                {
                    _context.Instructors.Remove(instructor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    TempData["ErrorMessage"] = "Неможливо видалити інструктора, оскільки за ним закріплені групи або заняття.";
                    return RedirectToAction(nameof(Delete), new { id });
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private bool InstructorExists(int id)
        {
            return _context.Instructors.Any(e => e.Id == id);
        }

        private async Task ValidateInstructorAsync(Instructor instructor)
        {
            instructor.FullName = (instructor.FullName ?? string.Empty).Trim();
            instructor.PhoneNumber = (instructor.PhoneNumber ?? string.Empty).Trim();
            instructor.LicenseSerial = (instructor.LicenseSerial ?? string.Empty).Trim();

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
        }

        private void RemoveNavigationModelState()
        {
            ModelState.Remove(nameof(Instructor.Groups));
            ModelState.Remove(nameof(Instructor.PracticeSessions));
            ModelState.Remove(nameof(Instructor.TheorySessions));
            ModelState.Remove(nameof(Instructor.CategoryNames));
        }
    }
}
