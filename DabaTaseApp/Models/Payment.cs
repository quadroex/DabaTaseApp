using System.ComponentModel.DataAnnotations;

namespace DabaTaseApp.Models;

public partial class Payment
{
    public int Id { get; set; }

    [Display(Name = "Учень")]
    public int StudentId { get; set; }

    [Display(Name = "Сума")]
    public int Amount { get; set; }

    [Display(Name = "Дата та час платежу")]
    public DateTime PaymentDate { get; set; }

    [Display(Name = "Учень")]
    public virtual Student Student { get; set; } = null!;
}