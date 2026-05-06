using DabaTaseApp.Validation;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace DabaTaseApp.Models;

public partial class Student
{
    public int Id { get; set; }

    [Display(Name = "Акаунт")]
    public string? ApplicationUserId { get; set; }

    [Display(Name = "ПІБ")]
    [Required(ErrorMessage = "Поле не повинно бути порожнім")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "ПІБ має містити від 3 до 120 символів.")]
    public string FullName { get; set; } = null!;

    [Display(Name = "Баланс")]
    [DecimalRange(-1000000, 1000000, ErrorMessage = "Баланс має бути в межах від -1 000 000 до 1 000 000 грн.")]
    public decimal Balance { get; set; }

    [Display(Name = "Цільова категорія")]
    [Required(ErrorMessage = "Оберіть цільову категорію.")]
    public string TargetCategory { get; set; } = null!;

    [Display(Name = "Група")]
    public int? GroupId { get; set; }

    [Display(Name = "Група")]
    public virtual Group? Group { get; set; }

    [Display(Name = "Акаунт")]
    public virtual IdentityUser? ApplicationUser { get; set; }

    [Display(Name = "Платежі")]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    [Display(Name = "Практичні заняття")]
    public virtual ICollection<PracticeSession> PracticeSessions { get; set; } = new List<PracticeSession>();
}
