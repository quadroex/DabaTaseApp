using DabaTaseApp.Models;
using DabaTaseApp.Models.ViewModels;
using DabaTaseApp.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DabaTaseApp.Controllers
{
    [Authorize(Roles = AppRoles.AdminOrStudent)]
    public class PaymentsController : Controller
    {
        private readonly Lab1Context _context;

        public PaymentsController(Lab1Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var query = _context.Payments
                .Include(p => p.Student)
                .OrderByDescending(p => p.PaymentDate)
                .AsQueryable();

            if (User.IsInRole(AppRoles.Student))
            {
                var student = await GetCurrentStudentAsync();
                if (student == null)
                {
                    TempData["ErrorMessage"] = "Ваш акаунт ще не прив'язаний до картки учня.";
                    return View(Array.Empty<Payment>());
                }

                query = query.Where(p => p.StudentId == student.Id);
            }

            return View(await query.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Student)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (payment == null)
            {
                return NotFound();
            }

            if (!await CanAccessPaymentAsync(payment))
            {
                return Forbid();
            }

            return View(payment);
        }

        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Create()
        {
            await PopulateStudentsAsync();
            return View(new Payment { PaymentDate = DateTime.Now });
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,StudentId,Amount,PaymentDate")] Payment payment)
        {
            ModelState.Remove(nameof(Payment.Student));
            ValidatePaymentDate(payment.PaymentDate);

            var student = await _context.Students.FindAsync(payment.StudentId);
            if (student == null)
            {
                ModelState.AddModelError(nameof(Payment.StudentId), "Оберіть учня.");
            }

            if (ModelState.IsValid && student != null)
            {
                student.Balance += payment.Amount;
                _context.Add(payment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await PopulateStudentsAsync(payment.StudentId);
            return View(payment);
        }

        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                return NotFound();
            }

            ViewBag.AmountDisplay = payment.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            await PopulateStudentsAsync(payment.StudentId);
            return View(payment);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StudentId,Amount,PaymentDate")] Payment payment)
        {
            if (id != payment.Id)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(Payment.Student));
            ValidatePaymentDate(payment.PaymentDate);

            var existingPayment = await _context.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existingPayment == null)
            {
                return NotFound();
            }

            var newStudent = await _context.Students.FindAsync(payment.StudentId);
            if (newStudent == null)
            {
                ModelState.AddModelError(nameof(Payment.StudentId), "Оберіть учня.");
            }

            if (ModelState.IsValid && newStudent != null)
            {
                if (existingPayment.StudentId == payment.StudentId)
                {
                    newStudent.Balance += payment.Amount - existingPayment.Amount;
                }
                else
                {
                    var oldStudent = await _context.Students.FindAsync(existingPayment.StudentId);
                    if (oldStudent != null)
                    {
                        oldStudent.Balance -= existingPayment.Amount;
                    }

                    newStudent.Balance += payment.Amount;
                }

                try
                {
                    _context.Update(payment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PaymentExists(payment.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            await PopulateStudentsAsync(payment.StudentId);
            return View(payment);
        }

        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Student)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                var student = await _context.Students.FindAsync(payment.StudentId);
                if (student != null)
                {
                    student.Balance -= payment.Amount;
                }

                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = AppRoles.Student)]
        public async Task<IActionResult> TopUp()
        {
            var student = await GetCurrentStudentAsync();
            if (student == null)
            {
                TempData["ErrorMessage"] = "Ваш акаунт ще не прив'язаний до картки учня.";
                return RedirectToAction("Index", "Home");
            }

            return View(new TopUpViewModel());
        }

        [Authorize(Roles = AppRoles.Student)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TopUp(TopUpViewModel model)
        {
            var student = await GetCurrentStudentAsync();
            if (student == null)
            {
                TempData["ErrorMessage"] = "Ваш акаунт ще не прив'язаний до картки учня.";
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var amount = model.Amount!.Value;
            student.Balance += amount;
            _context.Payments.Add(new Payment
            {
                StudentId = student.Id,
                Amount = amount,
                PaymentDate = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Баланс успішно поповнено.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateStudentsAsync(int? selectedStudentId = null)
        {
            ViewData["StudentId"] = new SelectList(
                await _context.Students.OrderBy(s => s.FullName).ToListAsync(),
                "Id",
                "FullName",
                selectedStudentId);
        }

        private async Task<Student?> GetCurrentStudentAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            return await _context.Students.FirstOrDefaultAsync(s => s.ApplicationUserId == userId);
        }

        private async Task<bool> CanAccessPaymentAsync(Payment payment)
        {
            if (User.IsInRole(AppRoles.Admin))
            {
                return true;
            }

            var student = await GetCurrentStudentAsync();
            return student != null && payment.StudentId == student.Id;
        }

        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(e => e.Id == id);
        }

        private void ValidatePaymentDate(DateTime paymentDate)
        {
            if (paymentDate > DateTime.Now.AddMinutes(5))
            {
                ModelState.AddModelError(nameof(Payment.PaymentDate), "Дата платежу не може бути в майбутньому.");
            }
        }
    }
}
