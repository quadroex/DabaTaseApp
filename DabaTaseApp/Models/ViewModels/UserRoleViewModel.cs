using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace DabaTaseApp.Models.ViewModels;

public class UserRoleViewModel
{
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string SelectedRole { get; set; } = string.Empty;

    public IReadOnlyList<string> Roles { get; set; } = [];

    public bool CanChangeRole { get; set; }

    public string ProfileStatus { get; set; } = string.Empty;

    public bool HasLinkedProfile { get; set; }
}

public class UsersIndexViewModel
{
    public IReadOnlyList<string> AvailableRoles { get; set; } = [];

    public IReadOnlyList<SelectListItem> CategoryOptions { get; set; } = [];

    public IReadOnlyList<SelectListItem> GroupOptions { get; set; } = [];

    public IReadOnlyList<SelectListItem> UnlinkedStudentOptions { get; set; } = [];

    public IReadOnlyList<SelectListItem> UnlinkedInstructorOptions { get; set; } = [];

    public CreateUserViewModel NewUser { get; set; } = new();

    public List<UserRoleViewModel> Users { get; set; } = [];
}

public class CreateUserViewModel
{
    [Required(ErrorMessage = "Вкажіть email.")]
    [EmailAddress(ErrorMessage = "Вкажіть коректний email.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть роль.")]
    public string Role { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть пароль.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль має містити щонайменше 6 символів.")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Паролі не збігаються.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Профіль")]
    public string ProfileMode { get; set; } = ProfileModes.CreateNew;

    [Display(Name = "Існуючий учень")]
    public int? ExistingStudentId { get; set; }

    [Display(Name = "Існуючий інструктор")]
    public int? ExistingInstructorId { get; set; }

    [Display(Name = "ПІБ")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "ПІБ має містити від 3 до 120 символів.")]
    public string? FullName { get; set; }

    [Display(Name = "Категорія")]
    public string? TargetCategory { get; set; }

    [Display(Name = "Група")]
    public int? GroupId { get; set; }

    [Display(Name = "Телефон")]
    [Phone(ErrorMessage = "Вкажіть коректний номер телефону.")]
    [StringLength(30, ErrorMessage = "Номер телефону надто довгий.")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Ліцензія")]
    [StringLength(40, MinimumLength = 3, ErrorMessage = "Ліцензія має містити від 3 до 40 символів.")]
    public string? LicenseSerial { get; set; }
}

public static class ProfileModes
{
    public const string CreateNew = "create";
    public const string LinkExisting = "link";
}
