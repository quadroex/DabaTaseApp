using System.ComponentModel.DataAnnotations;

namespace DabaTaseApp.Models.ViewModels;

public class TopUpViewModel
{
    [Display(Name = "Сума")]
    [Range(1, 1000000, ErrorMessage = "Сума поповнення має бути від 1 до 1 000 000 грн.")]
    public int Amount { get; set; }
}
