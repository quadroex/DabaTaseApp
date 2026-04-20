using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DabaTaseApp.Models;

public partial class Student
{
    public int Id { get; set; }

    [Display(Name = "ПІБ")]
    [Required(ErrorMessage = "Поле не повинно бути порожнім")]
    public string FullName { get; set; } = null!;

    [Display(Name = "Баланс")]
    public int Balance { get; set; }

    [Display(Name = "Цільова категорія")]
    public string TargetCategory { get; set; } = null!;

    [Display(Name = "Група")]
    public int? GroupId { get; set; }

    [Display(Name = "Група")]
    public virtual Group? Group { get; set; }

    [Display(Name = "Платежі")]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    [Display(Name = "Практичні заняття")]
    public virtual ICollection<PracticeSession> PracticeSessions { get; set; } = new List<PracticeSession>();
}