using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace DabaTaseApp.Models;

public partial class Instructor
{
    public int Id { get; set; }

    [Display(Name = "Акаунт")]
    public string? ApplicationUserId { get; set; }

    [Display(Name = "ПІБ")]
    [Required(ErrorMessage = "Поле не повинно бути порожнім")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "ПІБ має містити від 3 до 120 символів.")]
    public string FullName { get; set; } = null!;

    [Display(Name = "Номер телефону")]
    [Required(ErrorMessage = "Поле не повинно бути порожнім")]
    [Phone(ErrorMessage = "Вкажіть коректний номер телефону.")]
    [StringLength(30, ErrorMessage = "Номер телефону надто довгий.")]
    public string PhoneNumber { get; set; } = null!;

    [Display(Name = "Серія/номер ліцензії")]
    [Required(ErrorMessage = "Поле не повинно бути порожнім")]
    [StringLength(40, MinimumLength = 3, ErrorMessage = "Ліцензія має містити від 3 до 40 символів.")]
    public string LicenseSerial { get; set; } = null!;

    [Display(Name = "Групи")]
    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

    [Display(Name = "Практичні заняття")]
    public virtual ICollection<PracticeSession> PracticeSessions { get; set; } = new List<PracticeSession>();

    [Display(Name = "Теоретичні заняття")]
    public virtual ICollection<TheorySession> TheorySessions { get; set; } = new List<TheorySession>();

    [Display(Name = "Категорії")]
    public virtual ICollection<Category> CategoryNames { get; set; } = new List<Category>();

    [Display(Name = "Акаунт")]
    public virtual IdentityUser? ApplicationUser { get; set; }
}
