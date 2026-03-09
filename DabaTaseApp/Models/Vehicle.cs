using System.ComponentModel.DataAnnotations;

namespace DabaTaseApp.Models;

public partial class Vehicle
{
    [Display(Name = "Номерний знак")]
    public string PlateNumber { get; set; } = null!;

    [Display(Name = "Марка")]
    public string Mark { get; set; } = null!;

    [Display(Name = "Модель")]
    public string Model { get; set; } = null!;

    [Display(Name = "Активний")]
    public bool IsActive { get; set; }

    [Display(Name = "Практичні заняття")]
    public virtual ICollection<PracticeSession> PracticeSessions { get; set; } = new List<PracticeSession>();
}