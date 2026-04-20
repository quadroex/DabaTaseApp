using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DabaTaseApp.Models;
using ClosedXML.Excel;
using System.Text;

namespace DabaTaseApp.Controllers
{
    public class GroupsController : Controller
    {
        private readonly Lab1Context _context;

        public GroupsController(Lab1Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var groups = await _context.Groups.Include(g => g.TheoryInstructor).ToListAsync();
            return View(groups);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var @group = await _context.Groups
                .Include(g => g.TheoryInstructor)
                .Include(g => g.Students)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (@group == null) return NotFound();

            return View(@group);
        }

        [HttpGet]
        public async Task<IActionResult> Export()
        {
            var groups = await _context.Groups
                .Include(g => g.TheoryInstructor)
                .Include(g => g.Students)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Групи та Учні");
                var currentRow = 1;

                worksheet.Cell(currentRow, 1).Value = "Назва групи";
                worksheet.Cell(currentRow, 2).Value = "Дата початку";
                worksheet.Cell(currentRow, 3).Value = "Дата закінчення";
                worksheet.Cell(currentRow, 4).Value = "Інструктор (Теорія)";
                worksheet.Cell(currentRow, 5).Value = "ПІБ Учня";
                worksheet.Cell(currentRow, 6).Value = "Категорія";
                worksheet.Cell(currentRow, 7).Value = "Баланс (₴)";

                worksheet.Range(1, 1, 1, 7).Style.Font.Bold = true;
                worksheet.Range(1, 1, 1, 7).Style.Fill.BackgroundColor = XLColor.LightGray;

                foreach (var g in groups)
                {
                    if (g.Students.Any())
                    {
                        foreach (var s in g.Students)
                        {
                            currentRow++;
                            worksheet.Cell(currentRow, 1).Value = g.GroupName;
                            worksheet.Cell(currentRow, 2).Value = g.StartDate.ToString("dd.MM.yyyy");
                            worksheet.Cell(currentRow, 3).Value = g.EndDate.ToString("dd.MM.yyyy");
                            worksheet.Cell(currentRow, 4).Value = g.TheoryInstructor?.FullName ?? "Не призначено";
                            worksheet.Cell(currentRow, 5).Value = s.FullName;
                            worksheet.Cell(currentRow, 6).Value = s.TargetCategory;
                            worksheet.Cell(currentRow, 7).Value = s.Balance;
                        }
                    }
                    else
                    {
                        currentRow++;
                        worksheet.Cell(currentRow, 1).Value = g.GroupName;
                        worksheet.Cell(currentRow, 2).Value = g.StartDate.ToString("dd.MM.yyyy");
                        worksheet.Cell(currentRow, 3).Value = g.EndDate.ToString("dd.MM.yyyy");
                        worksheet.Cell(currentRow, 4).Value = g.TheoryInstructor?.FullName ?? "Не призначено";
                        worksheet.Cell(currentRow, 5).Value = "— БЕЗ УЧНІВ —";
                    }
                }

                worksheet.Columns().AdjustToContents();
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Full_Groups_Report.xlsx");
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportSingle(int id)
        {
            var group = await _context.Groups
                .Include(g => g.TheoryInstructor)
                .Include(g => g.Students)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null) return NotFound();

            using (var workbook = new XLWorkbook())
            {
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
                worksheet.Range(currentRow, 1, currentRow, 3).Style.Fill.BackgroundColor = XLColor.LightGray;

                foreach (var student in group.Students)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = student.FullName;
                    worksheet.Cell(currentRow, 2).Value = student.TargetCategory;
                    worksheet.Cell(currentRow, 3).Value = student.Balance;
                }

                worksheet.Columns().AdjustToContents();
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Group_{group.GroupName}_Report.xlsx");
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportStudents(int groupId, IFormFile fileExcel)
        {
            if (fileExcel == null || fileExcel.Length == 0) return RedirectToAction(nameof(Details), new { id = groupId });

            var validCategories = await _context.Categories.Select(c => c.Name).ToListAsync();
            var defaultCategory = validCategories.Contains("B") ? "B" : validCategories.FirstOrDefault();

            using (var stream = new MemoryStream())
            {
                await fileExcel.CopyToAsync(stream);
                using (var workbook = new XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheet(1);
                    var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                    foreach (var row in rows)
                    {
                        var name = row.Cell(1).GetValue<string>();
                        var category = row.Cell(2).GetValue<string>();

                        if (string.IsNullOrWhiteSpace(name)) continue;

                        if (string.IsNullOrWhiteSpace(category) || !validCategories.Contains(category))
                        {
                            category = defaultCategory;
                        }

                        var student = new Student
                        {
                            FullName = name,
                            TargetCategory = category,
                            GroupId = groupId,
                            Balance = 0
                        };
                        _context.Students.Add(student);
                    }
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Details), new { id = groupId });
        }

        public IActionResult Create()
        {
            ViewData["TheoryInstructorId"] = new SelectList(_context.Instructors, "Id", "FullName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,GroupName,StartDate,EndDate,TheoryInstructorId")] Group @group)
        {
            if (ModelState.IsValid)
            {
                _context.Add(@group);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["TheoryInstructorId"] = new SelectList(_context.Instructors, "Id", "FullName", @group.TheoryInstructorId);
            return View(@group);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var @group = await _context.Groups.FindAsync(id);
            if (@group == null) return NotFound();
            ViewData["TheoryInstructorId"] = new SelectList(_context.Instructors, "Id", "FullName", @group.TheoryInstructorId);
            return View(@group);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,GroupName,StartDate,EndDate,TheoryInstructorId")] Group @group)
        {
            if (id != @group.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(@group);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["TheoryInstructorId"] = new SelectList(_context.Instructors, "Id", "FullName", @group.TheoryInstructorId);
            return View(@group);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var @group = await _context.Groups.Include(g => g.TheoryInstructor).FirstOrDefaultAsync(m => m.Id == id);
            if (@group == null) return NotFound();
            return View(@group);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var @group = await _context.Groups.FindAsync(id);
            if (@group != null) _context.Groups.Remove(@group);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Http;
//using System.IO;
//using ClosedXML.Excel;
//using System.Text;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.EntityFrameworkCore;
//using DabaTaseApp.Models;

//namespace DabaTaseApp.Controllers
//{
//    public class GroupsController : Controller
//    {
//        private readonly Lab1Context _context;

//        public GroupsController(Lab1Context context)
//        {
//            _context = context;
//        }

//        // GET: Groups
//        public async Task<IActionResult> Index()
//        {
//            var lab1Context = _context.Groups.Include(g => g.TheoryInstructor);
//            return View(await lab1Context.ToListAsync());
//        }

//        // GET: Groups/Details/5
//        public async Task<IActionResult> Details(int? id)
//        {
//            if (id == null)
//            {
//                return NotFound();
//            }

//            var @group = await _context.Groups
//                .Include(g => g.TheoryInstructor)
//                .FirstOrDefaultAsync(m => m.Id == id);
//            if (@group == null)
//            {
//                return NotFound();
//            }

//            return RedirectToAction("Index", "Students", new { id = @group.Id, name = @group.GroupName });
//        }

//        // GET: Groups/Create
//        public IActionResult Create()
//        {
//            ViewData["TheoryInstructorId"] = new SelectList(_context.Instructors, "Id", "FullName");
//            return View();
//        }

//        // POST: Groups/Create
//        // To protect from overposting attacks, enable the specific properties you want to bind to.
//        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create([Bind("Id,GroupName,StartDate,EndDate,TheoryInstructorId")] Group @group)
//        {
//            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.Id == @group.TheoryInstructorId);
//            @group.TheoryInstructor = instructor;

//            ModelState.Clear();
//            TryValidateModel(@group);

//            if (ModelState.IsValid)
//            {
//                _context.Add(@group);
//                await _context.SaveChangesAsync();
//                return RedirectToAction(nameof(Index));
//            }
//            ViewData["TheoryInstructorId"] = new SelectList(_context.Instructors, "Id", "FullName", @group.TheoryInstructorId);
//            return View(@group);
//        }

//        // GET: Groups/Edit/5
//        public async Task<IActionResult> Edit(int? id)
//        {
//            if (id == null)
//            {
//                return NotFound();
//            }

//            var @group = await _context.Groups.FindAsync(id);
//            if (@group == null)
//            {
//                return NotFound();
//            }
//            ViewData["TheoryInstructorId"] = new SelectList(_context.Instructors, "Id", "FullName", @group.TheoryInstructorId);
//            return View(@group);
//        }

//        // POST: Groups/Edit/5
//        // To protect from overposting attacks, enable the specific properties you want to bind to.
//        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(int id, [Bind("Id,GroupName,TheoryInstructorId,StartDate,EndDate")] Group @group)
//        {
//            if (id != @group.Id)
//            {
//                return NotFound();
//            }

//            group.TheoryInstructor = await _context.Instructors.FirstOrDefaultAsync(i => i.Id == group.TheoryInstructorId);

//            ModelState.Clear();
//            TryValidateModel(group);

//            if (ModelState.IsValid)
//            {
//                try
//                {
//                    _context.Update(@group);
//                    await _context.SaveChangesAsync();
//                }
//                catch (DbUpdateConcurrencyException)
//                {
//                    if (!GroupExists(@group.Id))
//                    {
//                        return NotFound();
//                    }
//                    else
//                    {
//                        throw;
//                    }
//                }
//                return RedirectToAction(nameof(Index));
//            }
//            ViewData["TheoryInstructorId"] = new SelectList(_context.Instructors, "Id", "FullName", @group.TheoryInstructorId);
//            return View(@group);
//        }

//        // GET: Groups/Delete/5
//        public async Task<IActionResult> Delete(int? id)
//        {
//            if (id == null)
//            {
//                return NotFound();
//            }

//            var @group = await _context.Groups
//                .Include(g => g.TheoryInstructor)
//                .FirstOrDefaultAsync(m => m.Id == id);
//            if (@group == null)
//            {
//                return NotFound();
//            }

//            return View(@group);
//        }

//        // POST: Groups/Delete/5
//        [HttpPost, ActionName("Delete")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> DeleteConfirmed(int id)
//        {
//            var @group = await _context.Groups.FindAsync(id);
//            if (@group != null)
//            {
//                try
//                {
//                    _context.Groups.Remove(@group);
//                    await _context.SaveChangesAsync();
//                }
//                catch (DbUpdateException)
//                {
//                    TempData["ErrorMessage"] = "Неможливо видалити групу, оскільки до неї прив'язані учні або заняття. Спочатку видаліть їх.";
//                    return RedirectToAction(nameof(Delete), new { id = id });
//                }
//            }
//            return RedirectToAction(nameof(Index));
//        }

//        [HttpGet]
//        public async Task<IActionResult> Export()
//        {
//            var groups = await _context.Groups
//                .Include(g => g.TheoryInstructor)
//                .Include(g => g.Students)
//                .ToListAsync();

//            using (var workbook = new XLWorkbook())
//            {
//                var worksheet = workbook.Worksheets.Add("Групи та Учні");
//                var currentRow = 1;

//                worksheet.Cell(currentRow, 1).Value = "Назва групи";
//                worksheet.Cell(currentRow, 2).Value = "Дата початку";
//                worksheet.Cell(currentRow, 3).Value = "Дата закінчення";
//                worksheet.Cell(currentRow, 4).Value = "Інструктор (Теорія)";
//                worksheet.Cell(currentRow, 5).Value = "ПІБ Учня";
//                worksheet.Cell(currentRow, 6).Value = "Категорія";
//                worksheet.Cell(currentRow, 7).Value = "Баланс (₴)";

//                worksheet.Range(1, 1, 1, 7).Style.Font.Bold = true;
//                worksheet.Range(1, 1, 1, 7).Style.Fill.BackgroundColor = XLColor.LightGray;

//                foreach (var g in groups)
//                {
//                    if (g.Students.Any())
//                    {
//                        foreach (var s in g.Students)
//                        {
//                            currentRow++;
//                            worksheet.Cell(currentRow, 1).Value = g.GroupName;
//                            worksheet.Cell(currentRow, 2).Value = g.StartDate.ToString("dd.MM.yyyy");
//                            worksheet.Cell(currentRow, 3).Value = g.EndDate.ToString("dd.MM.yyyy");
//                            worksheet.Cell(currentRow, 4).Value = g.TheoryInstructor?.FullName ?? "Не призначено";
//                            worksheet.Cell(currentRow, 5).Value = s.FullName;
//                            worksheet.Cell(currentRow, 6).Value = s.TargetCategory;
//                            worksheet.Cell(currentRow, 7).Value = s.Balance;
//                        }
//                    }
//                    else
//                    {
//                        currentRow++;
//                        worksheet.Cell(currentRow, 1).Value = g.GroupName;
//                        worksheet.Cell(currentRow, 2).Value = g.StartDate.ToString("dd.MM.yyyy");
//                        worksheet.Cell(currentRow, 3).Value = g.EndDate.ToString("dd.MM.yyyy");
//                        worksheet.Cell(currentRow, 4).Value = g.TheoryInstructor?.FullName ?? "Не призначено";
//                        worksheet.Cell(currentRow, 5).Value = "— БЕЗ УЧНІВ —";
//                    }
//                }

//                worksheet.Columns().AdjustToContents();

//                using (var stream = new MemoryStream())
//                {
//                    workbook.SaveAs(stream);
//                    var content = stream.ToArray();
//                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Groups_Details_{DateTime.Now:ddMMyyyy}.xlsx");
//                }
//            }
//        }

//        [HttpGet]
//        public async Task<IActionResult> ExportSingle(int id)
//        {
//            var group = await _context.Groups
//                .Include(g => g.TheoryInstructor)
//                .Include(g => g.Students)
//                .FirstOrDefaultAsync(g => g.Id == id);

//            if (group == null) return NotFound();

//            using (var workbook = new XLWorkbook())
//            {
//                var worksheet = workbook.Worksheets.Add("Деталі групи");
//                worksheet.Cell(1, 1).Value = "Група:";
//                worksheet.Cell(1, 2).Value = group.GroupName;
//                worksheet.Cell(2, 1).Value = "Період:";
//                worksheet.Cell(2, 2).Value = $"{group.StartDate:dd.MM.yyyy} - {group.EndDate:dd.MM.yyyy}";
//                worksheet.Cell(3, 1).Value = "Інструктор:";
//                worksheet.Cell(3, 2).Value = group.TheoryInstructor?.FullName ?? "Не призначено";

//                worksheet.Range(1, 1, 3, 1).Style.Font.Bold = true;

//                var currentRow = 5;
//                worksheet.Cell(currentRow, 1).Value = "Список учнів";
//                worksheet.Range(currentRow, 1, currentRow, 3).Merge().Style.Font.Bold = true;

//                currentRow++;
//                worksheet.Cell(currentRow, 1).Value = "ПІБ Учня";
//                worksheet.Cell(currentRow, 2).Value = "Категорія";
//                worksheet.Cell(currentRow, 3).Value = "Баланс (₴)";
//                worksheet.Range(currentRow, 1, currentRow, 3).Style.Fill.BackgroundColor = XLColor.LightGray;

//                foreach (var student in group.Students)
//                {
//                    currentRow++;
//                    worksheet.Cell(currentRow, 1).Value = student.FullName;
//                    worksheet.Cell(currentRow, 2).Value = student.TargetCategory;
//                    worksheet.Cell(currentRow, 3).Value = student.Balance;
//                }

//                worksheet.Columns().AdjustToContents();
//                using (var stream = new MemoryStream())
//                {
//                    workbook.SaveAs(stream);
//                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Group_{group.GroupName}_Details.xlsx");
//                }
//            }
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Import(IFormFile fileExcel)
//        {
//            if (fileExcel == null || fileExcel.Length == 0) return RedirectToAction(nameof(Index));

//            var log = new StringBuilder();
//            log.AppendLine($"Журнал імпорту груп від {DateTime.Now}");
//            int success = 0, errors = 0;

//            using (var stream = new MemoryStream())
//            {
//                await fileExcel.CopyToAsync(stream);
//                using (var workbook = new XLWorkbook(stream))
//                {
//                    var worksheet = workbook.Worksheet(1);
//                    var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

//                    foreach (var row in rows)
//                    {
//                        var gName = row.Cell(1).GetValue<string>();
//                        var sDateStr = row.Cell(2).GetValue<string>();
//                        var eDateStr = row.Cell(3).GetValue<string>();
//                        var instName = row.Cell(4).GetValue<string>();

//                        if (string.IsNullOrWhiteSpace(gName))
//                        {
//                            log.AppendLine($"[Error] Рядок {row.RowNumber()}: Назва групи порожня.");
//                            errors++; continue;
//                        }

//                        if (_context.Groups.Any(g => g.GroupName == gName))
//                        {
//                            log.AppendLine($"[Warning] Рядок {row.RowNumber()}: Група '{gName}' вже існує.");
//                            errors++; continue;
//                        }

//                        if (!DateTime.TryParse(sDateStr, out DateTime sDate) || !DateTime.TryParse(eDateStr, out DateTime eDate))
//                        {
//                            log.AppendLine($"[Error] Рядок {row.RowNumber()}: Невірний формат дати.");
//                            errors++; continue;
//                        }

//                        var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.FullName == instName);
//                        if (instructor == null && !string.IsNullOrWhiteSpace(instName))
//                        {
//                            log.AppendLine($"[Warning] Рядок {row.RowNumber()}: Інструктора '{instName}' не знайдено.");
//                        }

//                        var newGroup = new Group
//                        {
//                            GroupName = gName,
//                            StartDate = DateOnly.FromDateTime(sDate),
//                            EndDate = DateOnly.FromDateTime(eDate),
//                            TheoryInstructorId = instructor?.Id
//                        };

//                        _context.Groups.Add(newGroup);
//                        log.AppendLine($"[Success] Рядок {row.RowNumber()}: Група '{gName}' додана.");
//                        success++;
//                    }
//                    await _context.SaveChangesAsync();
//                }
//            }

//            log.AppendLine($"\nПідсумок: успішно - {success}, помилок/пропусків - {errors}.");

//            if (errors > 0)
//            {
//                return File(Encoding.UTF8.GetBytes(log.ToString()), "text/plain", "Groups_Import_Log.txt");
//            }

//            return RedirectToAction(nameof(Index));
//        }

//        private bool GroupExists(int id)
//        {
//            return _context.Groups.Any(e => e.Id == id);
//        }
//    }
//}
