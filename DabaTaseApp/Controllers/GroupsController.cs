using ClosedXML.Excel;
using DabaTaseApp.Models;
using DabaTaseApp.Security;
using DabaTaseApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Group = DabaTaseApp.Models.Group;

namespace DabaTaseApp.Controllers
{
    [Authorize(Roles = AppRoles.AdminOrInstructor)]
    public class GroupsController : Controller
    {
        private readonly Lab1Context _context;
        private readonly UserManager<IdentityUser> _userManager;

        private const string ImportedStudentDefaultPassword = "123456";
        private static readonly Regex EmailRegex = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);

        public GroupsController(Lab1Context context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var query = _context.Groups
                .Include(g => g.TheoryInstructor)
                .OrderBy(g => g.GroupName)
                .AsQueryable();

            if (User.IsInRole(AppRoles.Instructor) && !User.IsInRole(AppRoles.Admin))
            {
                var instructor = await GetCurrentInstructorAsync();
                if (instructor == null)
                {
                    TempData["ErrorMessage"] = "Ваш акаунт ще не прив'язаний до картки інструктора.";
                    return View(Array.Empty<Group>());
                }

                query = query.Where(g => g.TheoryInstructorId == instructor.Id);
            }

            return View(await query.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var group = await _context.Groups
                .Include(g => g.TheoryInstructor)
                .Include(g => g.Students.OrderBy(s => s.FullName))
                .FirstOrDefaultAsync(m => m.Id == id);

            if (group == null)
                return NotFound();

            if (!await UserCanAccessGroupAsync(group))
                return Forbid();

            return View(group);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpGet]
        public async Task<IActionResult> Export()
        {
            var groups = await _context.Groups
                .Include(g => g.TheoryInstructor)
                .Include(g => g.Students)
                    .ThenInclude(s => s.ApplicationUser)
                .OrderBy(g => g.GroupName)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Групи та Учні");
            var currentRow = 1;

            worksheet.Cell(currentRow, 1).Value = "Назва групи";
            worksheet.Cell(currentRow, 2).Value = "Дата початку";
            worksheet.Cell(currentRow, 3).Value = "Дата закінчення";
            worksheet.Cell(currentRow, 4).Value = "Інструктор (Теорія)";
            worksheet.Cell(currentRow, 5).Value = "ПІБ Учня";
            worksheet.Cell(currentRow, 6).Value = "Категорія";
            worksheet.Cell(currentRow, 7).Value = "Баланс (₴)";
            worksheet.Cell(currentRow, 8).Value = "Email акаунта";

            worksheet.Range(1, 1, 1, 8).Style.Font.Bold = true;
            worksheet.Range(1, 1, 1, 8).Style.Fill.BackgroundColor = XLColor.LightGray;

            foreach (var group in groups)
            {
                if (group.Students.Any())
                {
                    foreach (var student in group.Students)
                    {
                        currentRow++;
                        worksheet.Cell(currentRow, 1).Value = group.GroupName;
                        worksheet.Cell(currentRow, 2).Value = group.StartDate.ToString("dd.MM.yyyy");
                        worksheet.Cell(currentRow, 3).Value = group.EndDate.ToString("dd.MM.yyyy");
                        worksheet.Cell(currentRow, 4).Value = group.TheoryInstructor?.FullName ?? "Не призначено";
                        worksheet.Cell(currentRow, 5).Value = student.FullName;
                        worksheet.Cell(currentRow, 6).Value = student.TargetCategory;
                        worksheet.Cell(currentRow, 7).Value = student.Balance;
                        worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "0.00";
                        worksheet.Cell(currentRow, 8).Value = student.ApplicationUser?.Email ?? "—";
                    }
                }
                else
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = group.GroupName;
                    worksheet.Cell(currentRow, 2).Value = group.StartDate.ToString("dd.MM.yyyy");
                    worksheet.Cell(currentRow, 3).Value = group.EndDate.ToString("dd.MM.yyyy");
                    worksheet.Cell(currentRow, 4).Value = group.TheoryInstructor?.FullName ?? "Не призначено";
                    worksheet.Cell(currentRow, 5).Value = "Без учнів";
                    worksheet.Cell(currentRow, 8).Value = "-";
                }
            }

            worksheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Full_Groups_Report.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportSingle(int id)
        {
            var group = await _context.Groups
                .Include(g => g.TheoryInstructor)
                .Include(g => g.Students)
                    .ThenInclude(s => s.ApplicationUser)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
                return NotFound();

            if (!await UserCanAccessGroupAsync(group))
                return Forbid();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Деталі групи");
            worksheet.Cell(1, 1).Value = "Група:";
            worksheet.Cell(1, 2).Value = group.GroupName;
            worksheet.Cell(2, 1).Value = "Період:";
            worksheet.Cell(2, 2).Value = $"{group.StartDate:dd.MM.yyyy} - {group.EndDate:dd.MM.yyyy}";
            worksheet.Cell(3, 1).Value = "Інструктор:";
            worksheet.Cell(3, 2).Value = group.TheoryInstructor?.FullName ?? "Не призначено";

            worksheet.Range(1, 1, 3, 1).Style.Font.Bold = true;

            var currentRow = 5;
            worksheet.Cell(currentRow, 1).Value = "Список учнів";
            worksheet.Range(currentRow, 1, currentRow, 4).Merge().Style.Font.Bold = true;

            currentRow++;
            worksheet.Cell(currentRow, 1).Value = "ПІБ Учня";
            worksheet.Cell(currentRow, 2).Value = "Категорія";
            worksheet.Cell(currentRow, 3).Value = "Баланс (₴)";
            worksheet.Cell(currentRow, 4).Value = "Email акаунта";
            worksheet.Range(currentRow, 1, currentRow, 4).Style.Fill.BackgroundColor = XLColor.LightGray;

            foreach (var student in group.Students)
            {
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = student.FullName;
                worksheet.Cell(currentRow, 2).Value = student.TargetCategory;
                worksheet.Cell(currentRow, 3).Value = student.Balance;
                worksheet.Cell(currentRow, 3).Style.NumberFormat.Format = "0.00";
                worksheet.Cell(currentRow, 4).Value = student.ApplicationUser?.Email ?? "-";
            }

            worksheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Group_{group.GroupName}_Report.xlsx");
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpGet]
        public IActionResult GroupTemplate()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Шаблон груп");

            ws.Cell(1, 1).Value = "Назва групи";
            ws.Cell(1, 2).Value = "Дата початку";
            ws.Cell(1, 3).Value = "Дата закінчення";
            ws.Cell(1, 4).Value = "Інструктор теорії";
            ws.Range(1, 1, 1, 4).Style.Font.Bold = true;
            ws.Range(1, 1, 1, 4).Style.Fill.BackgroundColor = XLColor.LightBlue;

            ws.Cell(2, 1).Value = "xx";
            ws.Cell(2, 2).Value = "01.09.2026";
            ws.Cell(2, 3).Value = "31.12.2026";
            ws.Cell(2, 4).Value = "Іванченко Іван Іванович";

            ws.Cell(1, 6).Value = "Формат дат: dd.MM.yyyy  |  Інструктор - необов'язково";
            ws.Cell(1, 6).Style.Font.Italic = true;
            ws.Cell(1, 6).Style.Font.FontColor = XLColor.Gray;

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Groups_Import_Template.xlsx");
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpGet]
        public async Task<IActionResult> StudentsTemplate()
        {
            var categories = await _context.Categories
                .Select(c => c.Name)
                .OrderBy(n => n)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Шаблон учнів");

            ws.Cell(1, 1).Value = "ПІБ учня";
            ws.Cell(1, 2).Value = "Категорія";
            ws.Cell(1, 3).Value = "Email акаунта";
            ws.Cell(1, 5).Value = "Доступні категорії:";
            ws.Range(1, 1, 1, 3).Style.Font.Bold = true;
            ws.Range(1, 1, 1, 3).Style.Fill.BackgroundColor = XLColor.LightBlue;
            ws.Cell(1, 5).Style.Font.Bold = true;

            ws.Cell(2, 1).Value = "Іваненко Іван Іванович";
            ws.Cell(2, 2).Value = categories.FirstOrDefault() ?? "B";
            ws.Cell(2, 3).Value = "ivan@example.com";

            ws.Cell(1, 4).Value = "Email — необов'язково. Якщо вказано — буде створено акаунт із паролем 123456.";
            ws.Cell(1, 4).Style.Font.Italic = true;
            ws.Cell(1, 4).Style.Font.FontColor = XLColor.Gray;

            for (int i = 0; i < categories.Count; i++)
                ws.Cell(i + 2, 5).Value = categories[i];

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Students_Import_Template.xlsx");
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile fileExcel)
        {
            if (fileExcel == null || fileExcel.Length == 0)
            {
                TempData["ErrorMessage"] = "Будь ласка, оберіть файл для імпорту.";
                return RedirectToAction(nameof(Index));
            }

            var log = new ImportLogBuilder();
            var header = $"Лог імпорту навчальних груп від {DateTime.Now:dd.MM.yyyy HH:mm:ss}.\nФайл: {fileExcel.FileName}";

            if (!string.Equals(Path.GetExtension(fileExcel.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                log.AddError("Файл повинен бути у форматі .xlsx.");
                return File(log.BuildBytes(header, savedToDb: false), "text/plain", BuildLogName("Groups_Import"));
            }

            var existingGroupNames = new HashSet<string>(
                await _context.Groups.Select(g => g.GroupName).ToListAsync(),
                StringComparer.OrdinalIgnoreCase);
            var processedInThisFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var allInstructors = await _context.Instructors.ToListAsync();

            var validGroups = new List<(int RowNumber, string GroupName, Group Entity)>();

            try
            {
                using var stream = new MemoryStream();
                await fileExcel.CopyToAsync(stream);
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var usedRange = worksheet.RangeUsed();

                if (usedRange == null)
                {
                    log.AddError("Аркуш порожній.");
                    return File(log.BuildBytes(header, savedToDb: false), "text/plain", BuildLogName("Groups_Import"));
                }

                // Non-fatal header check
                var gh1 = worksheet.Cell(1, 1).GetValue<string>()?.Trim() ?? "";
                var gh2 = worksheet.Cell(1, 2).GetValue<string>()?.Trim() ?? "";
                var gh3 = worksheet.Cell(1, 3).GetValue<string>()?.Trim() ?? "";
                var gh4 = worksheet.Cell(1, 4).GetValue<string>()?.Trim() ?? "";
                if (!string.Equals(gh1, "Назва групи", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(gh2, "Дата початку", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(gh3, "Дата закінчення", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(gh4, "Інструктор теорії", StringComparison.OrdinalIgnoreCase))
                {
                    log.AddWarning("Заголовки файлу відрізняються від очікуваних. Імпорт продовжено за позиціями колонок.");
                }

                var lastRow = usedRange.LastRow().RowNumber();
                if (lastRow < 2)
                {
                    log.AddError("Файл не містить жодного рядка з даними для імпорту.");
                    return File(log.BuildBytes(header, savedToDb: false), "text/plain", BuildLogName("Groups_Import"));
                }

                for (int r = 2; r <= lastRow; r++)
                {
                    var row = worksheet.Row(r);
                    var rn = r;

                    var groupName = row.Cell(1).GetValue<string>()?.Trim();
                    var instructorName = row.Cell(4).GetValue<string>()?.Trim();

                    if (string.IsNullOrWhiteSpace(groupName) &&
                        string.IsNullOrWhiteSpace(row.Cell(2).GetValue<string>()) &&
                        string.IsNullOrWhiteSpace(row.Cell(3).GetValue<string>()) &&
                        string.IsNullOrWhiteSpace(instructorName))
                    {
                        log.AddWarning($"Рядок {rn}: порожній рядок пропущено.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(groupName))
                    {
                        log.AddError($"Рядок {rn}, комірка A{rn}: назва групи порожня. Рядок пропущено.");
                        continue;
                    }

                    if (existingGroupNames.Contains(groupName))
                    {
                        log.AddError($"Рядок {rn}, комірка A{rn}: група '{groupName}' вже існує в базі даних. Рядок пропущено.");
                        continue;
                    }

                    if (processedInThisFile.Contains(groupName))
                    {
                        log.AddError($"Рядок {rn}, комірка A{rn}: група '{groupName}' дублюється в цьому файлі. Рядок пропущено.");
                        continue;
                    }

                    var startDate = ParseExcelDate(row.Cell(2));
                    if (startDate is null)
                    {
                        log.AddError($"Рядок {rn}, комірка B{rn}: невірний формат дати початку. Очікується dd.MM.yyyy. Рядок пропущено.");
                        continue;
                    }

                    var endDate = ParseExcelDate(row.Cell(3));
                    if (endDate is null)
                    {
                        log.AddError($"Рядок {rn}, комірка C{rn}: невірний формат дати закінчення. Очікується dd.MM.yyyy. Рядок пропущено.");
                        continue;
                    }

                    if (endDate < startDate)
                    {
                        log.AddError($"Рядок {rn}, комірки B{rn}-C{rn}: дата закінчення раніше дати початку. Рядок пропущено.");
                        continue;
                    }

                    int? instructorId = null;
                    if (!string.IsNullOrWhiteSpace(instructorName))
                    {
                        var instructor = allInstructors.FirstOrDefault(i =>
                            string.Equals(i.FullName, instructorName, StringComparison.OrdinalIgnoreCase));
                        if (instructor != null)
                            instructorId = instructor.Id;
                        else
                            log.AddWarning($"Рядок {rn}, комірка D{rn}: інструктора '{instructorName}' не знайдено. Групу буде збережено без викладача.");
                    }

                    processedInThisFile.Add(groupName);
                    validGroups.Add((rn, groupName, new Group
                    {
                        GroupName = groupName,
                        StartDate = startDate.Value,
                        EndDate = endDate.Value,
                        TheoryInstructorId = instructorId
                    }));
                }
            }
            catch (Exception ex)
            {
                log.AddFailure($"Не вдалося прочитати Excel-файл: {ex.Message}");
                return File(log.BuildBytes(header, savedToDb: false), "text/plain", BuildLogName("Groups_Import"));
            }

            bool savedToDb = false;
            if (validGroups.Count > 0)
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Groups.AddRange(validGroups.Select(v => v.Entity));
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                    savedToDb = true;
                    foreach (var v in validGroups)
                        log.AddSuccess($"Рядок {v.RowNumber}: Групу '{v.GroupName}' збережено в базу даних.");
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    log.AddFailure($"Помилка збереження в базу даних: {ex.Message}. Жодну групу не збережено.");
                }
            }

            if (!log.HasIssues && savedToDb)
            {
                TempData["SuccessMessage"] = $"Імпорт завершено. Додано груп: {log.SuccessCount}.";
                return RedirectToAction(nameof(Index));
            }

            return File(log.BuildBytes(header, savedToDb), "text/plain", BuildLogName("Groups_Import"));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportStudents(int groupId, IFormFile fileExcel)
        {
            if (fileExcel == null || fileExcel.Length == 0)
            {
                TempData["ErrorMessage"] = "Будь ласка, оберіть файл для імпорту учнів.";
                return RedirectToAction(nameof(Details), new { id = groupId });
            }

            var log = new ImportLogBuilder();

            if (!string.Equals(Path.GetExtension(fileExcel.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                log.AddError("Файл повинен бути у форматі .xlsx.");
                var extHeader = $"Лог імпорту учнів від {DateTime.Now:dd.MM.yyyy HH:mm:ss}.\nФайл: {fileExcel.FileName}";
                return File(log.BuildBytes(extHeader, savedToDb: false), "text/plain", BuildLogName("Students_Import"));
            }

            var group = await _context.Groups.FindAsync(groupId);
            if (group == null)
            {
                TempData["ErrorMessage"] = "Групу не знайдено.";
                return RedirectToAction(nameof(Index));
            }

            var header = $"Лог імпорту учнів у групу '{group.GroupName}' від {DateTime.Now:dd.MM.yyyy HH:mm:ss}.\nФайл: {fileExcel.FileName}";

            var categoryNames = await _context.Categories.Select(c => c.Name).ToListAsync();
            var categoryMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in categoryNames) categoryMap[c] = c;

            var existingStudentNames = new HashSet<string>(
                await _context.Students
                    .Where(s => s.GroupId == groupId)
                    .Select(s => s.FullName)
                    .ToListAsync(),
                StringComparer.OrdinalIgnoreCase);
            var processedNamesInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var processedEmailsInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var validStudents = new List<(int RowNumber, string Name, string Category, string? Email)>();

            try
            {
                using var stream = new MemoryStream();
                await fileExcel.CopyToAsync(stream);
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var usedRange = worksheet.RangeUsed();

                if (usedRange == null)
                {
                    log.AddError("Аркуш порожній.");
                    return File(log.BuildBytes(header, savedToDb: false), "text/plain", BuildLogName("Students_Import"));
                }

                var sh1 = worksheet.Cell(1, 1).GetValue<string>()?.Trim() ?? "";
                var sh2 = worksheet.Cell(1, 2).GetValue<string>()?.Trim() ?? "";
                if (!string.Equals(sh1, "ПІБ учня", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(sh2, "Категорія", StringComparison.OrdinalIgnoreCase))
                {
                    log.AddWarning("Заголовки файлу відрізняються від очікуваних (A1, B1). Імпорт продовжено за позиціями колонок.");
                }

                var sh3 = worksheet.Cell(1, 3).GetValue<string>()?.Trim() ?? "";
                var hasEmailColumn = string.Equals(sh3, "Email акаунта", StringComparison.OrdinalIgnoreCase);
                if (!hasEmailColumn && !string.IsNullOrWhiteSpace(sh3))
                    log.AddWarning("Заголовок колонки C (Email акаунта) не розпізнано. Email-колонка ігнорується.");

                var lastStudentRow = usedRange.LastRow().RowNumber();
                if (lastStudentRow < 2)
                {
                    log.AddError("Файл не містить жодного рядка з даними для імпорту.");
                    return File(log.BuildBytes(header, savedToDb: false), "text/plain", BuildLogName("Students_Import"));
                }

                for (int r = 2; r <= lastStudentRow; r++)
                {
                    var row = worksheet.Row(r);
                    var rn = r;
                    var rawName = row.Cell(1).GetValue<string>();
                    var categoryInput = row.Cell(2).GetValue<string>()?.Trim();
                    var rawEmail = hasEmailColumn ? row.Cell(3).GetValue<string>()?.Trim() : null;

                    var name = NormalizePersonName(rawName);

                    if (string.IsNullOrEmpty(name) && string.IsNullOrWhiteSpace(categoryInput) && string.IsNullOrWhiteSpace(rawEmail))
                    {
                        log.AddWarning($"Рядок {rn}: порожній рядок пропущено.");
                        continue;
                    }

                    if (string.IsNullOrEmpty(name))
                    {
                        log.AddError($"Рядок {rn}, комірка A{rn}: ПІБ учня порожнє. Рядок пропущено.");
                        continue;
                    }

                    if (existingStudentNames.Contains(name))
                    {
                        log.AddError($"Рядок {rn}, комірка A{rn}: учень '{name}' вже існує в цій групі. Рядок пропущено.");
                        continue;
                    }

                    if (processedNamesInFile.Contains(name))
                    {
                        log.AddError($"Рядок {rn}, комірка A{rn}: учень '{name}' дублюється в цьому файлі. Рядок пропущено.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(categoryInput) || !categoryMap.TryGetValue(categoryInput, out var canonicalCategory))
                    {
                        log.AddError($"Рядок {rn}, комірка B{rn}: категорія '{categoryInput}' не знайдена в системі. Рядок пропущено.");
                        continue;
                    }

                    string? validatedEmail = null;
                    if (!string.IsNullOrWhiteSpace(rawEmail))
                    {
                        if (!EmailRegex.IsMatch(rawEmail))
                        {
                            log.AddError($"Рядок {rn}, комірка C{rn}: невірний формат email '{rawEmail}'. Рядок пропущено.");
                            continue;
                        }

                        if (processedEmailsInFile.Contains(rawEmail))
                        {
                            log.AddError($"Рядок {rn}, комірка C{rn}: email '{rawEmail}' дублюється в цьому файлі. Рядок пропущено.");
                            continue;
                        }

                        var existingUser = await _userManager.FindByEmailAsync(rawEmail);
                        if (existingUser != null)
                        {
                            log.AddError($"Рядок {rn}, комірка C{rn}: email '{rawEmail}' вже зареєстровано в системі. Рядок пропущено.");
                            continue;
                        }

                        processedEmailsInFile.Add(rawEmail);
                        validatedEmail = rawEmail;
                    }

                    processedNamesInFile.Add(name);
                    validStudents.Add((rn, name, canonicalCategory, validatedEmail));
                }
            }
            catch (Exception ex)
            {
                log.AddFailure($"Не вдалося прочитати Excel-файл: {ex.Message}");
                return File(log.BuildBytes(header, savedToDb: false), "text/plain", BuildLogName("Students_Import"));
            }

            bool savedToDb = false;
            int accountsCreated = 0;
            if (validStudents.Count > 0)
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    foreach (var v in validStudents)
                    {
                        string? userId = null;
                        if (v.Email != null)
                        {
                            var newUser = new IdentityUser
                            {
                                UserName = v.Email,
                                Email = v.Email,
                                EmailConfirmed = true
                            };
                            var createResult = await _userManager.CreateAsync(newUser, ImportedStudentDefaultPassword);
                            if (!createResult.Succeeded)
                            {
                                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                                log.AddFailure($"Рядок {v.RowNumber}: не вдалося створити акаунт для '{v.Email}': {errors}. Жодного учня не збережено.");
                                await tx.RollbackAsync();
                                return File(log.BuildBytes(header, savedToDb: false), "text/plain", BuildLogName("Students_Import"));
                            }
                            var roleResult = await _userManager.AddToRoleAsync(newUser, AppRoles.Student);
                            if (!roleResult.Succeeded)
                            {
                                await _userManager.DeleteAsync(newUser);
                                var roleErrors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                                log.AddFailure($"Рядок {v.RowNumber}: не вдалося призначити роль для '{v.Email}': {roleErrors}. Жодного учня не збережено.");
                                await tx.RollbackAsync();
                                return File(log.BuildBytes(header, savedToDb: false), "text/plain", BuildLogName("Students_Import"));
                            }
                            userId = newUser.Id;
                            accountsCreated++;
                        }

                        _context.Students.Add(new Student
                        {
                            FullName = v.Name,
                            TargetCategory = v.Category,
                            GroupId = groupId,
                            Balance = 0,
                            ApplicationUserId = userId
                        });
                    }

                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                    savedToDb = true;

                    foreach (var v in validStudents)
                    {
                        if (v.Email != null)
                            log.AddSuccess($"Рядок {v.RowNumber}: Учня '{v.Name}' збережено. Створено акаунт {v.Email}.");
                        else
                            log.AddSuccess($"Рядок {v.RowNumber}: Учня '{v.Name}' збережено.");
                    }
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    log.AddFailure($"Помилка збереження в базу даних: {ex.Message}. Жодного учня не збережено.");
                }
            }

            if (!log.HasIssues && savedToDb)
            {
                TempData["SuccessMessage"] = $"Імпорт завершено. Додано учнів: {log.SuccessCount}.";
                return RedirectToAction(nameof(Details), new { id = groupId });
            }

            string? extraLine = accountsCreated > 0
                ? $"Створено акаунтів учнів: {accountsCreated}. Пароль за замовчуванням: {ImportedStudentDefaultPassword}"
                : null;
            return File(log.BuildBytes(header, savedToDb, extraLine), "text/plain", BuildLogName("Students_Import"));
        }

        [Authorize(Roles = AppRoles.Admin)]
        public IActionResult Create()
        {
            ViewData["TheoryInstructorId"] = new SelectList(_context.Instructors.OrderBy(i => i.FullName), "Id", "FullName");
            return View();
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,GroupName,StartDate,EndDate,TheoryInstructorId")] Group group)
        {
            await ValidateGroupAsync(group);

            if (ModelState.IsValid)
            {
                _context.Add(group);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Групу створено.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["TheoryInstructorId"] = new SelectList(_context.Instructors.OrderBy(i => i.FullName), "Id", "FullName", group.TheoryInstructorId);
            return View(group);
        }

        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var group = await _context.Groups.FindAsync(id);
            if (group == null)
                return NotFound();

            ViewData["TheoryInstructorId"] = new SelectList(_context.Instructors.OrderBy(i => i.FullName), "Id", "FullName", group.TheoryInstructorId);
            return View(group);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,GroupName,StartDate,EndDate,TheoryInstructorId")] Group group)
        {
            if (id != group.Id)
                return NotFound();

            await ValidateGroupAsync(group);

            if (ModelState.IsValid)
            {
                _context.Update(group);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Групу оновлено.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["TheoryInstructorId"] = new SelectList(_context.Instructors.OrderBy(i => i.FullName), "Id", "FullName", group.TheoryInstructorId);
            return View(group);
        }

        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var group = await _context.Groups
                .Include(g => g.TheoryInstructor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (group == null)
                return NotFound();

            return View(group);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var group = await _context.Groups.FindAsync(id);
            if (group == null)
                return RedirectToAction(nameof(Index));

            if (await _context.Students.AnyAsync(s => s.GroupId == id)
                || await _context.TheorySessions.AnyAsync(t => t.GroupId == id))
            {
                TempData["ErrorMessage"] = "Неможливо видалити групу, оскільки в ній є учні або теоретичні заняття.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Групу видалено.";
            return RedirectToAction(nameof(Index));
        }

        private static string NormalizePersonName(string? raw)
            => string.IsNullOrWhiteSpace(raw) ? string.Empty : Regex.Replace(raw.Trim(), @"\s+", " ");

        private static DateOnly? ParseExcelDate(IXLCell cell)
        {
            if (cell.TryGetValue<DateTime>(out var dt))
                return DateOnly.FromDateTime(dt);
            var str = cell.GetValue<string>()?.Trim();
            if (str != null && DateTime.TryParseExact(str,
                    ["dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd"],
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return DateOnly.FromDateTime(parsed);
            return null;
        }

        private static string BuildLogName(string prefix)
            => $"{prefix}_Report_{DateTime.Now:yyyyMMdd_HHmm}.txt";

        private async Task<Instructor?> GetCurrentInstructorAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrWhiteSpace(userId)
                ? null
                : await _context.Instructors.FirstOrDefaultAsync(i => i.ApplicationUserId == userId);
        }

        private async Task<bool> UserCanAccessGroupAsync(Group group)
        {
            if (User.IsInRole(AppRoles.Admin))
                return true;

            var instructor = await GetCurrentInstructorAsync();
            return instructor != null && group.TheoryInstructorId == instructor.Id;
        }

        private async Task ValidateGroupAsync(Group group)
        {
            group.GroupName = (group.GroupName ?? string.Empty).Trim();

            if (await _context.Groups.AnyAsync(g => g.Id != group.Id && g.GroupName == group.GroupName))
                ModelState.AddModelError(nameof(Group.GroupName), "Група з такою назвою вже існує.");

            if (group.TheoryInstructorId.HasValue &&
                !await _context.Instructors.AnyAsync(i => i.Id == group.TheoryInstructorId.Value))
                ModelState.AddModelError(nameof(Group.TheoryInstructorId), "Обраний інструктор не існує.");
        }
    }
}
