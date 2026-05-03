using DabaTaseApp.Models;
using DabaTaseApp.Models.ViewModels;
using DabaTaseApp.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DabaTaseApp.Controllers
{
    [Authorize(Policy = AppPolicies.Administration)]
    public class UsersController : Controller
    {
        private readonly Lab1Context _context;
        private readonly UserManager<IdentityUser> _userManager;

        public UsersController(Lab1Context context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            return View(await BuildIndexModelAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UsersIndexViewModel model)
        {
            NormalizeCreateUser(model.NewUser);
            ValidateRequestedRole(model.NewUser.Role, NewUserField(nameof(CreateUserViewModel.Role)));
            await ValidateProfileForNewUserAsync(model.NewUser);

            if (!ModelState.IsValid)
            {
                return View(nameof(Index), await BuildInvalidModelAsync(model.NewUser));
            }

            var user = new IdentityUser
            {
                UserName = model.NewUser.Email,
                Email = model.NewUser.Email,
                EmailConfirmed = true
            };

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var createResult = await _userManager.CreateAsync(user, model.NewUser.Password);
            if (!createResult.Succeeded)
            {
                AddIdentityErrors(createResult);
                return View(nameof(Index), await BuildInvalidModelAsync(model.NewUser));
            }

            var roleResult = await _userManager.AddToRoleAsync(user, model.NewUser.Role);
            if (!roleResult.Succeeded)
            {
                AddIdentityErrors(roleResult);
                return View(nameof(Index), await BuildInvalidModelAsync(model.NewUser));
            }

            if (model.NewUser.Role == AppRoles.Student)
            {
                _context.Students.Add(new Student
                {
                    ApplicationUserId = user.Id,
                    FullName = model.NewUser.FullName!,
                    TargetCategory = model.NewUser.TargetCategory ?? await GetDefaultCategoryAsync(),
                    Balance = 0
                });
            }
            else if (model.NewUser.Role == AppRoles.Instructor)
            {
                _context.Instructors.Add(new Instructor
                {
                    FullName = model.NewUser.FullName!,
                    PhoneNumber = model.NewUser.PhoneNumber!,
                    LicenseSerial = model.NewUser.LicenseSerial!
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] = "Користувача створено.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(string userId, string role)
        {
            role = NormalizeRole(role);
            if (!ValidateRequestedRole(role, nameof(role)))
            {
                TempData["ErrorMessage"] = "Обрана роль не підтримується системою.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Contains(AppRoles.Admin))
            {
                TempData["ErrorMessage"] = "Права доступу адміністратора не можна змінювати через цю сторінку.";
                return RedirectToAction(nameof(Index));
            }

            var rolesToRemove = currentRoles.Where(r => r != role).ToArray();
            if (rolesToRemove.Length > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    TempData["ErrorMessage"] = string.Join(" ", removeResult.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Index));
                }
            }

            if (!await _userManager.IsInRoleAsync(user, role))
            {
                var addResult = await _userManager.AddToRoleAsync(user, role);
                if (!addResult.Succeeded)
                {
                    TempData["ErrorMessage"] = string.Join(" ", addResult.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Index));
                }
            }

            var notes = await ReconcileProfilesAfterRoleChangeAsync(user.Id, currentRoles, role);
            TempData["SuccessMessage"] = notes.Count == 0
                ? "Роль користувача оновлено."
                : $"Роль користувача оновлено. {string.Join(" ", notes)}";

            return RedirectToAction(nameof(Index));
        }

        private async Task<UsersIndexViewModel> BuildIndexModelAsync()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.Email)
                .ToListAsync();

            var linkedStudents = await _context.Students
                .Where(s => s.ApplicationUserId != null)
                .Select(s => new { s.ApplicationUserId, s.FullName })
                .ToDictionaryAsync(s => s.ApplicationUserId!, s => s.FullName);

            var model = new UsersIndexViewModel
            {
                AvailableRoles = AppRoles.All,
                CategoryOptions = await BuildCategoryOptionsAsync(),
                NewUser = new CreateUserViewModel
                {
                    Role = AppRoles.Student,
                    TargetCategory = await GetDefaultCategoryAsync()
                }
            };

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                model.Users.Add(new UserRoleViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    Roles = roles.ToList(),
                    SelectedRole = roles.FirstOrDefault() ?? AppRoles.Student,
                    CanChangeRole = !roles.Contains(AppRoles.Admin),
                    ProfileStatus = BuildProfileStatus(user.Id, roles, linkedStudents)
                });
            }

            return model;
        }

        private async Task<UsersIndexViewModel> BuildInvalidModelAsync(CreateUserViewModel newUser)
        {
            var model = await BuildIndexModelAsync();
            model.NewUser = newUser;
            return model;
        }

        private async Task<IReadOnlyList<SelectListItem>> BuildCategoryOptionsAsync()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => c.Name)
                .ToListAsync();

            if (categories.Count == 0)
            {
                categories = ["B"];
            }

            return categories.Select(c => new SelectListItem(c, c)).ToList();
        }

        private async Task<string> GetDefaultCategoryAsync()
        {
            return await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => c.Name)
                .FirstOrDefaultAsync() ?? "B";
        }

        private async Task ValidateProfileForNewUserAsync(CreateUserViewModel newUser)
        {
            if (newUser.Role is AppRoles.Student or AppRoles.Instructor && string.IsNullOrWhiteSpace(newUser.FullName))
            {
                ModelState.AddModelError(NewUserField(nameof(CreateUserViewModel.FullName)), "Для цієї ролі потрібно вказати ПІБ.");
            }

            if (newUser.Role == AppRoles.Student)
            {
                if (string.IsNullOrWhiteSpace(newUser.TargetCategory))
                {
                    ModelState.AddModelError(NewUserField(nameof(CreateUserViewModel.TargetCategory)), "Для учня потрібно обрати категорію.");
                }
                else if (await _context.Categories.AnyAsync()
                    && !await _context.Categories.AnyAsync(c => c.Name == newUser.TargetCategory))
                {
                    ModelState.AddModelError(NewUserField(nameof(CreateUserViewModel.TargetCategory)), "Обрана категорія не існує.");
                }

                if (!string.IsNullOrWhiteSpace(newUser.FullName)
                    && await _context.Students.AnyAsync(s => s.FullName == newUser.FullName))
                {
                    ModelState.AddModelError(NewUserField(nameof(CreateUserViewModel.FullName)), "Учень із таким ПІБ уже існує.");
                }
            }

            if (newUser.Role == AppRoles.Instructor)
            {
                if (string.IsNullOrWhiteSpace(newUser.PhoneNumber))
                {
                    ModelState.AddModelError(NewUserField(nameof(CreateUserViewModel.PhoneNumber)), "Для інструктора потрібно вказати телефон.");
                }

                if (string.IsNullOrWhiteSpace(newUser.LicenseSerial))
                {
                    ModelState.AddModelError(NewUserField(nameof(CreateUserViewModel.LicenseSerial)), "Для інструктора потрібно вказати ліцензію.");
                }

                if (!string.IsNullOrWhiteSpace(newUser.PhoneNumber)
                    && await _context.Instructors.AnyAsync(i => i.PhoneNumber == newUser.PhoneNumber))
                {
                    ModelState.AddModelError(NewUserField(nameof(CreateUserViewModel.PhoneNumber)), "Інструктор із таким телефоном уже існує.");
                }

                if (!string.IsNullOrWhiteSpace(newUser.LicenseSerial)
                    && await _context.Instructors.AnyAsync(i => i.LicenseSerial == newUser.LicenseSerial))
                {
                    ModelState.AddModelError(NewUserField(nameof(CreateUserViewModel.LicenseSerial)), "Інструктор із такою ліцензією вже існує.");
                }
            }
        }

        private async Task<List<string>> ReconcileProfilesAfterRoleChangeAsync(
            string userId,
            IEnumerable<string> previousRoles,
            string newRole)
        {
            var notes = new List<string>();

            if (previousRoles.Contains(AppRoles.Student) && newRole != AppRoles.Student)
            {
                var note = await DetachStudentProfileAsync(userId);
                if (note != null)
                {
                    notes.Add(note);
                }
            }

            if (newRole == AppRoles.Student && !await _context.Students.AnyAsync(s => s.ApplicationUserId == userId))
            {
                notes.Add("Прив'яжіть акаунт до картки учня на сторінці учнів.");
            }

            if (newRole == AppRoles.Instructor)
            {
                notes.Add("Картка інструктора створюється окремо; точна прив'язка інструктора до акаунта потребує погодженої зміни БД.");
            }

            await _context.SaveChangesAsync();
            return notes;
        }

        private async Task<string?> DetachStudentProfileAsync(string userId)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.ApplicationUserId == userId);
            if (student == null)
            {
                return null;
            }

            var hasHistory = student.Balance != 0
                || student.GroupId != null
                || await _context.Payments.AnyAsync(p => p.StudentId == student.Id)
                || await _context.PracticeSessions.AnyAsync(p => p.StudentId == student.Id);

            if (!hasHistory && LooksLikeEmail(student.FullName))
            {
                _context.Students.Remove(student);
                return "Порожню картку учня з email замість ПІБ видалено.";
            }

            student.ApplicationUserId = null;
            return "Картку учня відв'язано від акаунта.";
        }

        private static string BuildProfileStatus(
            string userId,
            IEnumerable<string> roles,
            IReadOnlyDictionary<string, string> linkedStudents)
        {
            if (roles.Contains(AppRoles.Student))
            {
                return linkedStudents.TryGetValue(userId, out var studentName)
                    ? $"Учень: {studentName}"
                    : "Немає картки учня";
            }

            if (roles.Contains(AppRoles.Instructor))
            {
                return "Роль instructor";
            }

            return roles.Contains(AppRoles.Admin) ? "Адміністратор" : "Немає профілю";
        }

        private bool ValidateRequestedRole(string role, string fieldName)
        {
            if (AppRoles.All.Contains(role))
            {
                return true;
            }

            ModelState.AddModelError(fieldName, "Обрана роль не підтримується системою.");
            return false;
        }

        private static void NormalizeCreateUser(CreateUserViewModel newUser)
        {
            newUser.Email = newUser.Email.Trim();
            newUser.Role = NormalizeRole(newUser.Role);
            newUser.FullName = string.IsNullOrWhiteSpace(newUser.FullName) ? null : newUser.FullName.Trim();
            newUser.TargetCategory = string.IsNullOrWhiteSpace(newUser.TargetCategory) ? null : newUser.TargetCategory.Trim();
            newUser.PhoneNumber = string.IsNullOrWhiteSpace(newUser.PhoneNumber) ? null : newUser.PhoneNumber.Trim();
            newUser.LicenseSerial = string.IsNullOrWhiteSpace(newUser.LicenseSerial) ? null : newUser.LicenseSerial.Trim();
        }

        private static bool LooksLikeEmail(string value)
        {
            return value.Contains('@') && value.Contains('.');
        }

        private static string NormalizeRole(string? role)
        {
            return role?.Trim().ToLowerInvariant() ?? string.Empty;
        }

        private static string NewUserField(string propertyName)
        {
            return $"{nameof(UsersIndexViewModel.NewUser)}.{propertyName}";
        }

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}
