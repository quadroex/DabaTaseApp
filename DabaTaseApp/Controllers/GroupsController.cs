using ClosedXML.Excel;
using DabaTaseApp.Models;
using DabaTaseApp.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace DabaTaseApp.Controllers
{
    [Authorize(Roles = AppRoles.AdminOrInstructor)]
    public class GroupsController : Controller
    {
        private readonly Lab1Context _context;

        public GroupsController(Lab1Context context)
        {
            _context = context;
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
            {
                return NotFound();
            }

            var group = await _context.Groups
                .Include(g => g.TheoryInstructor)
                .Include(g => g.Students.OrderBy(s => s.FullName))
                .FirstOrDefaultAsync(m => m.Id == id);

            if (group == null)
            {
                return NotFound();
            }

            if (!await UserCanAccessGroupAsync(group))
            {
                return Forbid();
            }

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
                    worksheet.Cell(currentRow, 5).Value = "— БЕЗ УЧНІВ —";
                    worksheet.Cell(currentRow, 8).Value = "—";
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
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
            {
                return NotFound();
            }

            if (!await UserCanAccessGroupAsync(group))
            {
                return Forbid();
            }

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
            worksheet.Range(currentRow, 1, currentRow, 3).Merge().Style.Font.Bold = true;

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
                worksheet.Cell(currentRow, 4).Value = student.ApplicationUser?.Email ?? "—";
            }

            worksheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Group_{group.GroupName}_Report.xlsx");
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

            var log = new StringBuilder();
            log.AppendLine($"Лог імпорту навчальних груп від {DateTime.Now:dd.MM.yyyy HH:mm:ss}.");
            log.AppendLine($"Файл: {fileExcel.FileName}\n");

            var success = 0;
            var errors = 0;
            var warnings = 0;

            var existingGroups = await _context.Groups.Select(g => g.GroupName).ToListAsync();
            var processedInThisFile = new HashSet<string>();

            try
            {
                using var stream = new MemoryStream();
                await fileExcel.CopyToAsync(stream);
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var usedRange = worksheet.RangeUsed();
                if (usedRange == null)
                {
                    log.AppendLine("[ПОМИЛКА] Аркуш порожній.");
                    errors++;
                    log.AppendLine($"\nПІДСУМОК");
                    log.AppendLine($"Успішно: {success}");
                    log.AppendLine($"Пропущено: {errors}");
                    log.AppendLine($"Попереджень: {warnings}");
                    return File(Encoding.UTF8.GetBytes(log.ToString()), "text/plain", $"Groups_Import_Report_{DateTime.Now:yyyyMMdd_HHmm}.txt");
                }

                var rows = usedRange.RowsUsed().Skip(1);

                var rowIndex = 1;

                foreach (var row in rows)
                {
                    rowIndex++;

                    var groupName = row.Cell(1).GetValue<string>()?.Trim();
                    var startDateText = row.Cell(2).GetValue<string>()?.Trim();
                    var endDateText = row.Cell(3).GetValue<string>()?.Trim();
                    var instructorName = row.Cell(4).GetValue<string>()?.Trim();

                    if (string.IsNullOrWhiteSpace(groupName))
                    {
                        log.AppendLine($"[ПОМИЛКА] Рядок {rowIndex}: Назва групи порожня.");
                        errors++;
                        continue;
                    }

                    if (existingGroups.Contains(groupName))
                    {
                        log.AppendLine($"[ПОМИЛКА] Рядок {rowIndex}: Група з назвою '{groupName}' вже існує.");
                        errors++;
                        continue;
                    }

                    if (processedInThisFile.Contains(groupName))
                    {
                        log.AppendLine($"[ПОМИЛКА] Рядок {rowIndex}: Група '{groupName}' дублюється в цьому ж файлі.");
                        errors++;
                        continue;
                    }

                    if (!DateTime.TryParse(startDateText, out var startDate) || !DateTime.TryParse(endDateText, out var endDate))
                    {
                        log.AppendLine($"[ПОМИЛКА] Рядок {rowIndex}: Група '{groupName}' має невірний формат дати.");
                        errors++;
                        continue;
                    }

                    if (endDate < startDate)
                    {
                        log.AppendLine($"[ПОМИЛКА] Рядок {rowIndex}: Група '{groupName}' - дата закінчення раніше дати початку.");
                        errors++;
                        continue;
                    }

                    int? instructorId = null;
                    if (!string.IsNullOrWhiteSpace(instructorName))
                    {
                        var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.FullName == instructorName);
                        if (instructor != null)
                        {
                            instructorId = instructor.Id;
                        }
                        else
                        {
                            log.AppendLine($"[ПОПЕРЕДЖЕННЯ] Рядок {rowIndex}: Інструктора '{instructorName}' не знайдено. Групу створено без викладача.");
                            warnings++;
                        }
                    }

                    _context.Groups.Add(new Group
                    {
                        GroupName = groupName,
                        StartDate = DateOnly.FromDateTime(startDate),
                        EndDate = DateOnly.FromDateTime(endDate),
                        TheoryInstructorId = instructorId
                    });

                    processedInThisFile.Add(groupName);
                    existingGroups.Add(groupName);
                    log.AppendLine($"[УСПІХ] Рядок {rowIndex}: Групу '{groupName}' успішно збережено.");
                    success++;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                log.AppendLine($"\n[ЗБІЙ] Помилка обробки файлу: {ex.Message}");
                errors++;
            }

            log.AppendLine($"\nПІДСУМОК");
            log.AppendLine($"Успішно: {success}");
            log.AppendLine($"Пропущено: {errors}");
            log.AppendLine($"Попереджень: {warnings}");

            if (errors > 0 || warnings > 0)
            {
                return File(Encoding.UTF8.GetBytes(log.ToString()), "text/plain", $"Groups_Import_Report_{DateTime.Now:yyyyMMdd_HHmm}.txt");
            }

            return RedirectToAction(nameof(Index));
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

            var group = await _context.Groups.FindAsync(groupId);
            var groupDisplayName = group?.GroupName ?? groupId.ToString();

            var log = new StringBuilder();
            log.AppendLine($"Лог імпорту учнів у групу {groupDisplayName} від {DateTime.Now:dd.MM.yyyy HH:mm:ss}.");
            log.AppendLine($"Файл: {fileExcel.FileName}\n");

            var success = 0;
            var errors = 0;

            var validCategories = await _context.Categories.Select(c => c.Name).ToListAsync();
            var existingStudentsInGroup = await _context.Students
                .Where(s => s.GroupId == groupId)
                .Select(s => s.FullName)
                .ToListAsync();

            var processedInThisFile = new HashSet<string>();

            try
            {
                using var stream = new MemoryStream();
                await fileExcel.CopyToAsync(stream);
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var usedRange = worksheet.RangeUsed();
                if (usedRange == null)
                {
                    log.AppendLine("[ПОМИЛКА] Аркуш порожній.");
                    errors++;
                    log.AppendLine($"\nПІДСУМОК");
                    log.AppendLine($"Успішно: {success}");
                    log.AppendLine($"Пропущено: {errors}");
                    log.AppendLine($"Попереджень: 0");
                    return File(Encoding.UTF8.GetBytes(log.ToString()), "text/plain", $"Students_Import_Report_{DateTime.Now:yyyyMMdd_HHmm}.txt");
                }

                var rows = usedRange.RowsUsed().Skip(1);

                var rowIndex = 1;

                foreach (var row in rows)
                {
                    rowIndex++;

                    var name = row.Cell(1).GetValue<string>()?.Trim();
                    var category = row.Cell(2).GetValue<string>()?.Trim();

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        log.AppendLine($"[ПОМИЛКА] Рядок {rowIndex}: ім'я учня порожнє.");
                        errors++;
                        continue;
                    }

                    if (existingStudentsInGroup.Contains(name))
                    {
                        log.AppendLine($"[ПОМИЛКА] Рядок {rowIndex}: Учень '{name}' вже існує в цій групі.");
                        errors++;
                        continue;
                    }

                    if (processedInThisFile.Contains(name))
                    {
                        log.AppendLine($"[ПОМИЛКА] Рядок {rowIndex}: Учень '{name}' дублюється в цьому файлі.");
                        errors++;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(category) || !validCategories.Contains(category))
                    {
                        log.AppendLine($"[ПОМИЛКА] Рядок {rowIndex}: Категорія '{category}' недійсна або порожня.");
                        errors++;
                        continue;
                    }

                    _context.Students.Add(new Student
                    {
                        FullName = name,
                        TargetCategory = category,
                        GroupId = groupId,
                        Balance = 0,
                        ApplicationUserId = null
                    });

                    processedInThisFile.Add(name);
                    existingStudentsInGroup.Add(name);
                    log.AppendLine($"[УСПІХ] Рядок {rowIndex}: Учня '{name}' ({category}) збережено.");
                    success++;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                log.AppendLine($"\n[ЗБІЙ] Помилка обробки файлу: {ex.Message}");
                errors++;
            }

            log.AppendLine($"\nПІДСУМОК");
            log.AppendLine($"Успішно: {success}");
            log.AppendLine($"Пропущено: {errors}");

            if (errors > 0)
            {
                return File(Encoding.UTF8.GetBytes(log.ToString()), "text/plain", $"Students_Import_Report_{DateTime.Now:yyyyMMdd_HHmm}.txt");
            }

            return RedirectToAction(nameof(Details), new { id = groupId });
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
            {
                return NotFound();
            }

            var group = await _context.Groups.FindAsync(id);
            if (group == null)
            {
                return NotFound();
            }

            ViewData["TheoryInstructorId"] = new SelectList(_context.Instructors.OrderBy(i => i.FullName), "Id", "FullName", group.TheoryInstructorId);
            return View(group);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,GroupName,StartDate,EndDate,TheoryInstructorId")] Group group)
        {
            if (id != group.Id)
            {
                return NotFound();
            }

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
            {
                return NotFound();
            }

            var group = await _context.Groups
                .Include(g => g.TheoryInstructor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (group == null)
            {
                return NotFound();
            }

            return View(group);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var group = await _context.Groups.FindAsync(id);
            if (group == null)
            {
                return RedirectToAction(nameof(Index));
            }

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
            {
                return true;
            }

            var instructor = await GetCurrentInstructorAsync();
            return instructor != null && group.TheoryInstructorId == instructor.Id;
        }

        private async Task ValidateGroupAsync(Group group)
        {
            group.GroupName = (group.GroupName ?? string.Empty).Trim();

            if (await _context.Groups.AnyAsync(g => g.Id != group.Id && g.GroupName == group.GroupName))
            {
                ModelState.AddModelError(nameof(Group.GroupName), "Група з такою назвою вже існує.");
            }

            if (group.TheoryInstructorId.HasValue &&
                !await _context.Instructors.AnyAsync(i => i.Id == group.TheoryInstructorId.Value))
            {
                ModelState.AddModelError(nameof(Group.TheoryInstructorId), "Обраний інструктор не існує.");
            }
        }
    }
}
