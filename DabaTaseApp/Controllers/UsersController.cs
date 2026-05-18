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

            await AttachProfileForNewUserAsync(user.Id, model.NewUser);

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkStudentProfile(string userId, int studentId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (!await _userManager.IsInRoleAsync(user, AppRoles.Student))
            {
                TempData["ErrorMessage"] = "До картки учня можна прив'язати тільки акаунт із роллю student.";
                return RedirectToAction(nameof(Index));
            }

            var student = await _context.Students.FindAsync(studentId);
            if (student == null || student.ApplicationUserId != null)
            {
                TempData["ErrorMessage"] = "Оберіть неприв'язану картку учня.";
                return RedirectToAction(nameof(Index));
            }

            await DetachStudentProfileAsync(userId, save: false);
            student.ApplicationUserId = userId;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Картку учня прив'язано.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkInstructorProfile(string userId, int instructorId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (!await _userManager.IsInRoleAsync(user, AppRoles.Instructor))
            {
                TempData["ErrorMessage"] = "До картки інструктора можна прив'язати тільки акаунт із роллю instructor.";
                return RedirectToAction(nameof(Index));
            }

            var instructor = await _context.Instructors.FindAsync(instructorId);
            if (instructor == null || instructor.ApplicationUserId != null)
            {
                TempData["ErrorMessage"] = "Оберіть неприв'язану картку інструктора.";
                return RedirectToAction(nameof(Index));
            }

            await DetachInstructorProfileAsync(userId, save: false);
            instructor.ApplicationUserId = userId;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Картку інструктора прив'язано.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlinkProfile(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            await DetachStudentProfileAsync(userId, save: false);
            await DetachInstructorProfileAsync(userId, save: false);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Профіль відв'язано від акаунта.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId)
            {
                TempData["ErrorMessage"] = "Власний акаунт не можна видалити.";
                return RedirectToAction(nameof(Index));
            }

            if (string.Equals(user.Email, SuperAdmin.Email, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Акаунт суперадміністратора не можна видаляти.";
                return RedirectToAction(nameof(Index));
            }

            if (await _userManager.IsInRoleAsync(user, AppRoles.Admin) && !SuperAdmin.IsSuperAdmin(User))
            {
                TempData["ErrorMessage"] = "Акаунти адміністраторів може видаляти тільки суперадміністратор.";
                return RedirectToAction(nameof(Index));
            }

            var isLinked = await _context.Students.AnyAsync(s => s.ApplicationUserId == id)
                        || await _context.Instructors.AnyAsync(i => i.ApplicationUserId == id);

            if (isLinked)
            {
                TempData["ErrorMessage"] = "Спочатку відв'яжіть акаунт від картки учня або інструктора.";
                return RedirectToAction(nameof(Index));
            }

            var roles = await _userManager.GetRolesAsync(user);
            return View(new UserRoleViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                Roles = [.. roles],
                HasLinkedProfile = false,
                ProfileStatus = roles.Contains(AppRoles.Admin) ? "Адміністратор" : "Немає профілю"
            });
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId)
            {
                TempData["ErrorMessage"] = "Власний акаунт не можна видалити.";
                return RedirectToAction(nameof(Index));
            }

            if (string.Equals(user.Email, SuperAdmin.Email, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Акаунт суперадміністратора не можна видаляти.";
                return RedirectToAction(nameof(Index));
            }

            if (await _userManager.IsInRoleAsync(user, AppRoles.Admin) && !SuperAdmin.IsSuperAdmin(User))
            {
                TempData["ErrorMessage"] = "Акаунти адміністраторів може видаляти тільки суперадміністратор.";
                return RedirectToAction(nameof(Index));
            }

            var isLinked = await _context.Students.AnyAsync(s => s.ApplicationUserId == id)
                        || await _context.Instructors.AnyAsync(i => i.ApplicationUserId == id);
            if (isLinked)
            {
                TempData["ErrorMessage"] = "Спочатку відв'яжіть акаунт від картки учня або інструктора.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Акаунт видалено.";
            }
            else
            {
                TempData["ErrorMessage"] = "Помилка видалення: " + string.Join(" ", result.Errors.Select(e => e.Description));
            }
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

            var linkedInstructors = await _context.Instructors
                .Where(i => i.ApplicationUserId != null)
                .Select(i => new { i.ApplicationUserId, i.FullName })
                .ToDictionaryAsync(i => i.ApplicationUserId!, i => i.FullName);

            var model = new UsersIndexViewModel
            {
                AvailableRoles = AppRoles.All,
                CategoryOptions = await BuildCategoryOptionsAsync(),
                GroupOptions = await BuildGroupOptionsAsync(),
                UnlinkedStudentOptions = await BuildUnlinkedStudentOptionsAsync(),
                UnlinkedInstructorOptions = await BuildUnlinkedInstructorOptionsAsync(),
                NewUser = new CreateUserViewModel
                {
                    Role = AppRoles.Student,
                    ProfileMode = ProfileModes.CreateNew,
                    TargetCategory = await GetDefaultCategoryAsync()
                }
            };

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var profileStatus = BuildProfileStatus(user.Id, roles, linkedStudents, linkedInstructors, out var hasLinkedProfile);

                model.Users.Add(new UserRoleViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    Roles = roles.ToList(),
                    SelectedRole = roles.FirstOrDefault() ?? string.Empty,
                    CanChangeRole = !roles.Contains(AppRoles.Admin),
                    ProfileStatus = profileStatus,
                    HasLinkedProfile = hasLinkedProfile
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

        private async Task<IReadOnlyList<SelectListItem>> BuildGroupOptionsAsync()
        {
            return await _context.Groups
                .OrderBy(g => g.GroupName)
                .Select(g => new SelectListItem(g.GroupName, g.Id.ToString()))
                .ToListAsync();
        }

        private async Task<IReadOnlyList<SelectListItem>> BuildUnlinkedStudentOptionsAsync()
        {
            return await _context.Students
                .Where(s => s.ApplicationUserId == null)
                .OrderBy(s => s.FullName)
                .Select(s => new SelectListItem($"{s.FullName} ({s.TargetCategory})", s.Id.ToString()))
                .ToListAsync();
        }

        private async Task<IReadOnlyList<SelectListItem>> BuildUnlinkedInstructorOptionsAsync()
        {
            return await _context.Instructors
                .Where(i => i.ApplicationUserId == null)
                .OrderBy(i => i.FullName)
                .Select(i => new SelectListItem($"{i.FullName} ({i.LicenseSerial})", i.Id.ToString()))
                .ToListAsync();
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
            if (newUser.Role == AppRoles.Admin)
            {
                return;
            }

            var linkExisting = newUser.ProfileMode == ProfileModes.LinkExisting;

            if (newUser.Role == AppRoles.Student)
            {
                if (linkExisting)
                {
                    var student = newUser.ExistingStudentId.HasValue
                        ? await _context.Students.FindAsync(newUser.ExistingStudentId.Value)
                        : null;

                    if (student == null || student.ApplicationUserId != null)
                    {
                        ModelState.AddModelError(NewUserField(nameof(CreateUserViewModel.ExistingStudentId)), "Оберіть неприв'язану картку учня.");
                    }

                    return;
                }

                if (string.IsNullOrWhiteSpace(newUser.FullName))
                {
                    ModelState.AddModelError(NewUserField(nameof(CreateUserViewModel.FullName)), "Для учня потрібно вказати ПІБ.");
                }

                if (string.IsNullOrWhiteSpace(newUser.TargetCategory))
                {
                    ModelState.AddModelError(NewUserField(nameof(CreateUserViewModel.TargetCategory)), "Для учня потрібно обрати категорію.");
                }

                if (newUser.GroupId.HasValue &&
                    !await _context.Groups.AnyAsync(g => g.Id == newUser.GroupId.Value))
                {
                    ModelState.AddModelError(NewUserField(nameof(CreateUserViewModel.GroupId)), "Обрана група не існує.");
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
                if (linkExisting)
                {
                    var instructor = newUser.ExistingInstructorId.HasValue
                        ? await _context.Instructors.FindAsync(newUser.ExistingInstructorId.Value)
                        : null;

                    if (instructor == null || instructor.ApplicationUserId != null)
                    {
                        ModelState.AddModelError(NewUserField(nameof(CreateUserViewModel.ExistingInstructorId)), "Оберіть неприв'язану картку інструктора.");
                    }

                    return;
                }

                if (string.IsNullOrWhiteSpace(newUser.FullName))
                {
                    ModelState.AddModelError(NewUserField(nameof(CreateUserViewModel.FullName)), "Для інструктора потрібно вказати ПІБ.");
                }

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

        private async Task AttachProfileForNewUserAsync(string userId, CreateUserViewModel newUser)
        {
            if (newUser.Role == AppRoles.Student)
            {
                if (newUser.ProfileMode == ProfileModes.LinkExisting && newUser.ExistingStudentId.HasValue)
                {
                    var student = await _context.Students.FindAsync(newUser.ExistingStudentId.Value);
                    if (student != null)
                    {
                        student.ApplicationUserId = userId;
                    }

                    return;
                }

                _context.Students.Add(new Student
                {
                    ApplicationUserId = userId,
                    FullName = newUser.FullName!,
                    TargetCategory = newUser.TargetCategory ?? await GetDefaultCategoryAsync(),
                    GroupId = newUser.GroupId,
                    Balance = 0
                });
            }

            if (newUser.Role == AppRoles.Instructor)
            {
                if (newUser.ProfileMode == ProfileModes.LinkExisting && newUser.ExistingInstructorId.HasValue)
                {
                    var instructor = await _context.Instructors.FindAsync(newUser.ExistingInstructorId.Value);
                    if (instructor != null)
                    {
                        instructor.ApplicationUserId = userId;
                    }

                    return;
                }

                _context.Instructors.Add(new Instructor
                {
                    ApplicationUserId = userId,
                    FullName = newUser.FullName!,
                    PhoneNumber = newUser.PhoneNumber!,
                    LicenseSerial = newUser.LicenseSerial!
                });
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
                var note = await DetachStudentProfileAsync(userId, save: false);
                if (note != null)
                {
                    notes.Add(note);
                }
            }

            if (previousRoles.Contains(AppRoles.Instructor) && newRole != AppRoles.Instructor)
            {
                var note = await DetachInstructorProfileAsync(userId, save: false);
                if (note != null)
                {
                    notes.Add(note);
                }
            }

            if (newRole == AppRoles.Student && !await _context.Students.AnyAsync(s => s.ApplicationUserId == userId))
            {
                notes.Add("Прив'яжіть акаунт до картки учня.");
            }

            if (newRole == AppRoles.Instructor && !await _context.Instructors.AnyAsync(i => i.ApplicationUserId == userId))
            {
                notes.Add("Прив'яжіть акаунт до картки інструктора.");
            }

            await _context.SaveChangesAsync();
            return notes;
        }

        private async Task<string?> DetachStudentProfileAsync(string userId, bool save)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.ApplicationUserId == userId);
            if (student == null)
            {
                return null;
            }

            student.ApplicationUserId = null;
            if (save)
            {
                await _context.SaveChangesAsync();
            }

            return "Картку учня відв'язано від акаунта.";
        }

        private async Task<string?> DetachInstructorProfileAsync(string userId, bool save)
        {
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.ApplicationUserId == userId);
            if (instructor == null)
            {
                return null;
            }

            instructor.ApplicationUserId = null;
            if (save)
            {
                await _context.SaveChangesAsync();
            }

            return "Картку інструктора відв'язано від акаунта.";
        }

        private static string BuildProfileStatus(
            string userId,
            IEnumerable<string> roles,
            IReadOnlyDictionary<string, string> linkedStudents,
            IReadOnlyDictionary<string, string> linkedInstructors,
            out bool hasLinkedProfile)
        {
            hasLinkedProfile = false;

            if (roles.Contains(AppRoles.Student))
            {
                hasLinkedProfile = linkedStudents.TryGetValue(userId, out var studentName);
                return hasLinkedProfile ? $"Учень: {studentName}" : "Немає картки учня";
            }

            if (roles.Contains(AppRoles.Instructor))
            {
                hasLinkedProfile = linkedInstructors.TryGetValue(userId, out var instructorName);
                return hasLinkedProfile ? $"Інструктор: {instructorName}" : "Немає картки інструктора";
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
            newUser.ProfileMode = newUser.ProfileMode == ProfileModes.LinkExisting
                ? ProfileModes.LinkExisting
                : ProfileModes.CreateNew;
            newUser.FullName = string.IsNullOrWhiteSpace(newUser.FullName) ? null : newUser.FullName.Trim();
            newUser.TargetCategory = string.IsNullOrWhiteSpace(newUser.TargetCategory) ? null : newUser.TargetCategory.Trim();
            newUser.PhoneNumber = string.IsNullOrWhiteSpace(newUser.PhoneNumber) ? null : newUser.PhoneNumber.Trim();
            newUser.LicenseSerial = string.IsNullOrWhiteSpace(newUser.LicenseSerial) ? null : newUser.LicenseSerial.Trim();
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
