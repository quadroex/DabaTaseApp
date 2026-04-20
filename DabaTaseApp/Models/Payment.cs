using System.ComponentModel.DataAnnotations;

namespace DabaTaseApp.Models;

public partial class Payment
{
    public int Id { get; set; }

    [Display(Name = "Учень")]
    public int StudentId { get; set; }

    [Display(Name = "Сума")]
    [Range(0.01, 1000000.00, ErrorMessage = "Сума платежу має бути додатньою.")]
    public int Amount { get; set; }

    [Display(Name = "Дата та час платежу")]
    public DateTime PaymentDate { get; set; }

    [Display(Name = "Учень")]
    public virtual Student Student { get; set; } = null!;
}