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
    [Authorize(Roles = AppRoles.AllAuthenticated)]
    public class StudentsController : Controller
    {
        private readonly Lab1Context _context;
        private readonly UserManager<IdentityUser> _userManager;

        public StudentsController(Lab1Context context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var query = _context.Students
                .Include(s => s.Group)
                .Include(s => s.ApplicationUser)
                .OrderBy(s => s.FullName)
                .AsQueryable();

            if (User.IsInRole(AppRoles.Student))
            {
                var student = await GetCurrentStudentAsync();
                if (student == null)
                {
                    TempData["ErrorMessage"] = "Ваш акаунт ще не прив'язаний до картки учня.";
                    return View(Array.Empty<Student>());
                }

                query = student.GroupId.HasValue
                    ? query.Where(s => s.GroupId == student.GroupId)
                    : query.Where(s => s.Id == student.Id);
                ViewBag.CurrentStudentId = student.Id;
            }
            else if (User.IsInRole(AppRoles.Instructor) && !User.IsInRole(AppRoles.Admin))
            {
                var instructor = await GetCurrentInstructorAsync();
                if (instructor == null)
                {
                    TempData["ErrorMessage"] = "Ваш акаунт ще не прив'язаний до картки інструктора.";
                    return View(Array.Empty<Student>());
                }

                query = query.Where(s => s.Group != null && s.Group.TheoryInstructorId == instructor.Id);
            }

            return View(await query.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.Group)
                .Include(s => s.ApplicationUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (student == null)
            {
                return NotFound();
            }

            if (!await UserCanAccessStudentDetailsAsync(student))
            {
                return Forbid();
            }

            ViewBag.ShowBalance = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Student);
            return View(student);
        }

        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Create(int? categoryId)
        {
            await PopulateStudentFormAsync(new Student { GroupId = categoryId });
            return View();
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ApplicationUserId,FullName,Balance,TargetCategory,GroupId")] Student student)
        {
            await ValidateStudentAsync(student);
            RemoveNavigationModelState();

            if (ModelState.IsValid)
            {
                _context.Add(student);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Учня створено.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateStudentFormAsync(student);
            return View(student);
        }

        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            await PopulateStudentFormAsync(student);
            return View(student);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ApplicationUserId,FullName,Balance,TargetCategory,GroupId")] Student student)
        {
            if (id != student.Id)
            {
                return NotFound();
            }

            await ValidateStudentAsync(student);
            RemoveNavigationModelState();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(student);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                TempData["SuccessMessage"] = "Картку учня оновлено.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateStudentFormAsync(student);
            return View(student);
        }

        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.Group)
                .Include(s => s.ApplicationUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return RedirectToAction(nameof(Index));
            }

            if (await _context.Payments.AnyAsync(p => p.StudentId == id)
                || await _context.PracticeSessions.AnyAsync(p => p.StudentId == id))
            {
                TempData["ErrorMessage"] = "Неможливо видалити учня, оскільки в нього є платежі або практичні заняття.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Учня видалено.";
            return RedirectToAction(nameof(Index));
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

        private async Task<bool> UserCanAccessStudentDetailsAsync(Student student)
        {
            if (User.IsInRole(AppRoles.Admin))
            {
                return true;
            }

            if (User.IsInRole(AppRoles.Student))
            {
                var currentStudent = await GetCurrentStudentAsync();
                return currentStudent != null && currentStudent.Id == student.Id;
            }

            if (User.IsInRole(AppRoles.Instructor))
            {
                var instructor = await GetCurrentInstructorAsync();
                return instructor != null
                    && student.GroupId.HasValue
                    && await _context.Groups.AnyAsync(g => g.Id == student.GroupId && g.TheoryInstructorId == instructor.Id);
            }

            return false;
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Id == id);
        }

        private void RemoveNavigationModelState()
        {
            ModelState.Remove(nameof(Student.ApplicationUser));
            ModelState.Remove(nameof(Student.Group));
            ModelState.Remove(nameof(Student.Payments));
            ModelState.Remove(nameof(Student.PracticeSessions));
        }

        private async Task PopulateStudentFormAsync(Student student)
        {
            ViewData["GroupId"] = new SelectList(
                await _context.Groups.OrderBy(g => g.GroupName).ToListAsync(),
                "Id",
                "GroupName",
                student.GroupId);

            ViewData["TargetCategory"] = new SelectList(
                await _context.Categories.OrderBy(c => c.Name).ToListAsync(),
                "Name",
                "Name",
                student.TargetCategory);

            var studentUsers = await _userManager.GetUsersInRoleAsync(AppRoles.Student);
            var linkedUserIds = await _context.Students
                .Where(s => s.Id != student.Id && s.ApplicationUserId != null)
                .Select(s => s.ApplicationUserId!)
                .ToListAsync();

            var availableUsers = studentUsers
                .Where(u => !linkedUserIds.Contains(u.Id) || u.Id == student.ApplicationUserId)
                .OrderBy(u => u.Email)
                .ToList();

            ViewData["ApplicationUserId"] = new SelectList(availableUsers, "Id", "Email", student.ApplicationUserId);
        }

        private async Task ValidateStudentAsync(Student student)
        {
            student.FullName = (student.FullName ?? string.Empty).Trim();

            if (!await _context.Categories.AnyAsync(c => c.Name == student.TargetCategory))
            {
                ModelState.AddModelError(nameof(Student.TargetCategory), "Оберіть існуючу категорію.");
            }

            if (student.GroupId.HasValue && !await _context.Groups.AnyAsync(g => g.Id == student.GroupId.Value))
            {
                ModelState.AddModelError(nameof(Student.GroupId), "Обрана група не існує.");
            }

            if (await _context.Students.AnyAsync(s => s.Id != student.Id && s.FullName == student.FullName))
            {
                ModelState.AddModelError(nameof(Student.FullName), "Учень із таким ПІБ уже існує.");
            }

            await ValidateApplicationUserLinkAsync(student);
        }

        private async Task ValidateApplicationUserLinkAsync(Student student)
        {
            if (string.IsNullOrWhiteSpace(student.ApplicationUserId))
            {
                student.ApplicationUserId = null;
                return;
            }

            var user = await _userManager.FindByIdAsync(student.ApplicationUserId);
            if (user == null)
            {
                ModelState.AddModelError(nameof(Student.ApplicationUserId), "Обраний акаунт не існує.");
                return;
            }

            if (!await _userManager.IsInRoleAsync(user, AppRoles.Student))
            {
                ModelState.AddModelError(nameof(Student.ApplicationUserId), "До картки учня можна прив'язати тільки акаунт із роллю student.");
                return;
            }

            var alreadyLinked = await _context.Students
                .AnyAsync(s => s.Id != student.Id && s.ApplicationUserId == student.ApplicationUserId);

            if (alreadyLinked)
            {
                ModelState.AddModelError(nameof(Student.ApplicationUserId), "Цей акаунт уже прив'язаний до іншого учня.");
            }
        }
    }
}
