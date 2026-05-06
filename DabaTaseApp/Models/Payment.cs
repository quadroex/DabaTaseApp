using DabaTaseApp.Validation;
using System.ComponentModel.DataAnnotations;

namespace DabaTaseApp.Models;

public partial class Payment
{
    public int Id { get; set; }

    [Display(Name = "Учень")]
    public int StudentId { get; set; }

    [Display(Name = "Сума")]
    [DecimalRange(0.01, 1000000, ErrorMessage = "Сума платежу має бути від 0.01 до 1 000 000 грн.")]
    public decimal Amount { get; set; }

    [Display(Name = "Дата та час платежу")]
    public DateTime PaymentDate { get; set; }

    [Display(Name = "Учень")]
    public virtual Student Student { get; set; } = null!;
}
