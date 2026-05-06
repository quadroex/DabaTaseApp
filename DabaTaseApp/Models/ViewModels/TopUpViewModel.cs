using DabaTaseApp.Validation;
using System.ComponentModel.DataAnnotations;

namespace DabaTaseApp.Models.ViewModels;

public class TopUpViewModel
{
    [Display(Name = "Сума")]
    [DecimalRange(0.01, 1000000, ErrorMessage = "Сума поповнення має бути від 0.01 до 1 000 000 грн.")]
    public decimal Amount { get; set; }
}
