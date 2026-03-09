using System.ComponentModel.DataAnnotations;

namespace DabaTaseApp.Models;

public partial class Instructor
{
    public int Id { get; set; }

    [Display(Name = "ПІБ")]
    public string FullName { get; set; } = null!;

    [Display(Name = "Номер телефону")]
    public string PhoneNumber { get; set; } = null!;

    [Display(Name = "Серія/Номер ліцензії")]
    public string LicenseSerial { get; set; } = null!;

    [Display(Name = "Групи")]
    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

    [Display(Name = "Практичні заняття")]
    public virtual ICollection<PracticeSession> PracticeSessions { get; set; } = new List<PracticeSession>();

    [Display(Name = "Теоретичні заняття")]
    public virtual ICollection<TheorySession> TheorySessions { get; set; } = new List<TheorySession>();

    [Display(Name = "Категорії")]
    public virtual ICollection<Category> CategoryNames { get; set; } = new List<Category>();
}