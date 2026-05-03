using System.ComponentModel.DataAnnotations;

namespace DabaTaseApp.Models;

public partial class Vehicle
{
    [Display(Name = "Номерний знак")]
    [Required(ErrorMessage = "Поле не повинно бути порожнім")]
    [StringLength(20, MinimumLength = 4, ErrorMessage = "Номерний знак має містити від 4 до 20 символів.")]
    public string PlateNumber { get; set; } = null!;

    [Display(Name = "Марка")]
    [Required(ErrorMessage = "Поле не повинно бути порожнім")]
    [StringLength(60, ErrorMessage = "Марка надто довга.")]
    public string Mark { get; set; } = null!;

    [Display(Name = "Модель")]
    [Required(ErrorMessage = "Поле не повинно бути порожнім")]
    [StringLength(60, ErrorMessage = "Модель надто довга.")]
    public string Model { get; set; } = null!;

    [Display(Name = "Активний")]
    public bool IsActive { get; set; }

    [Display(Name = "Практичні заняття")]
    public virtual ICollection<PracticeSession> PracticeSessions { get; set; } = new List<PracticeSession>();
}
