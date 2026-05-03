using System.ComponentModel.DataAnnotations;
using DabaTaseApp.Models;
using DabaTaseApp.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DabaTaseApp.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly Lab1Context _context;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public RegisterModel(
            Lab1Context context,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string ReturnUrl { get; set; } = string.Empty;

        public IList<SelectListItem> CategoryOptions { get; set; } = [];

        public class InputModel
        {
            [Required(ErrorMessage = "Вкажіть повне ім'я.")]
            [Display(Name = "ПІБ")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Оберіть категорію.")]
            [Display(Name = "Цільова категорія")]
            public string TargetCategory { get; set; } = string.Empty;

            [Required(ErrorMessage = "Вкажіть email.")]
            [EmailAddress(ErrorMessage = "Вкажіть коректний email.")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Вкажіть пароль.")]
            [StringLength(100, ErrorMessage = "Пароль має містити від {2} до {1} символів.", MinimumLength = 8)]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Паролі не збігаються.")]
            [Display(Name = "Підтвердження пароля")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            await LoadCategoriesAsync();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ReturnUrl = returnUrl;
            await LoadCategoriesAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = new IdentityUser
            {
                UserName = Input.Email,
                Email = Input.Email
            };

            var result = await _userManager.CreateAsync(user, Input.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, AppRoles.Student);

                _context.Students.Add(new Student
                {
                    ApplicationUserId = user.Id,
                    FullName = Input.FullName.Trim(),
                    TargetCategory = Input.TargetCategory,
                    Balance = 0
                });
                await _context.SaveChangesAsync();

                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(returnUrl);
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        private async Task LoadCategoriesAsync()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => c.Name)
                .ToListAsync();

            if (categories.Count == 0)
            {
                categories = ["B", "C", "D"];
            }

            CategoryOptions = categories
                .Select(c => new SelectListItem(c, c))
                .ToList();

            if (string.IsNullOrWhiteSpace(Input.TargetCategory))
            {
                Input.TargetCategory = categories[0];
            }
        }
    }
}
