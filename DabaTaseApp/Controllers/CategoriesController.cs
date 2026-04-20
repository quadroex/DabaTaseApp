using Microsoft.AspNetCore.Http;
using System.IO;
using ClosedXML.Excel;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DabaTaseApp.Models;

namespace DabaTaseApp.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly Lab1Context _context;

        public CategoriesController(Lab1Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Categories.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories.FirstOrDefaultAsync(m => m.Id == id);
            if (category == null) return NotFound();

            return View(category);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description")] Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] Category category)
        {
            if (id != category.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories.FirstOrDefaultAsync(m => m.Id == id);
            if (category == null) return NotFound();

            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null) _context.Categories.Remove(category);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Export()
        {
            var categories = await _context.Categories.ToListAsync();
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Categories");
                var currentRow = 1;
                worksheet.Cell(currentRow, 1).Value = "Name";
                worksheet.Cell(currentRow, 2).Value = "Description";
                worksheet.Range(1, 1, 1, 2).Style.Font.Bold = true;

                foreach (var category in categories)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = category.Name;
                    worksheet.Cell(currentRow, 2).Value = category.Description;
                }
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Categories_{DateTime.Now:ddMMyyyy}.xlsx");
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile fileExcel)
        {
            if (fileExcel == null || fileExcel.Length == 0) return RedirectToAction(nameof(Index));

            var logBuilder = new System.Text.StringBuilder();
            logBuilder.AppendLine($"--- Журнал імпорту від {DateTime.Now} ---");
            int successCount = 0;
            int errorCount = 0;

            try
            {
                using (var stream = new MemoryStream())
                {
                    await fileExcel.CopyToAsync(stream);
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                        int rowIndex = 1;
                        foreach (var row in rows)
                        {
                            rowIndex++;
                            var name = row.Cell(1).GetValue<string>();
                            var description = row.Cell(2).GetValue<string>();

                            if (string.IsNullOrWhiteSpace(name))
                            {
                                logBuilder.AppendLine($"[ПОМИЛКА] Рядок {rowIndex}: Назва категорії порожня. Запис проігноровано.");
                                errorCount++;
                                continue;
                            }

                            if (_context.Categories.Any(c => c.Name == name))
                            {
                                logBuilder.AppendLine($"[ПОПЕРЕДЖЕННЯ] Рядок {rowIndex}: Категорія '{name}' вже існує. Запис проігноровано.");
                                errorCount++;
                                continue;
                            }

                            _context.Categories.Add(new Category { Name = name, Description = description });
                            logBuilder.AppendLine($"[УСПІХ] Рядок {rowIndex}: Додано категорію '{name}'.");
                            successCount++;
                        }
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                logBuilder.AppendLine($"[КРИТИЧНИЙ ЗБІЙ] Помилка читання файлу: {ex.Message}");
                errorCount++;
            }

            logBuilder.AppendLine($"\n--- Підсумок ---");
            logBuilder.AppendLine($"Успішно додано: {successCount}");
            logBuilder.AppendLine($"Пропущено/Помилок: {errorCount}");

            if (errorCount > 0)
            {
                var logBytes = System.Text.Encoding.UTF8.GetBytes(logBuilder.ToString());
                return File(logBytes, "text/plain", $"Import_Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}