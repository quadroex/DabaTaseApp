using System.ComponentModel.DataAnnotations;

namespace DabaTaseApp.Models.ViewModels;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Вкажіть поточний пароль.")]
    [DataType(DataType.Password)]
    [Display(Name = "Поточний пароль")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть новий пароль.")]
    [DataType(DataType.Password)]
    [Display(Name = "Новий пароль")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль має містити щонайменше 6 символів.")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Підтвердження нового пароля")]
    [Compare(nameof(NewPassword), ErrorMessage = "Паролі не збігаються.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
