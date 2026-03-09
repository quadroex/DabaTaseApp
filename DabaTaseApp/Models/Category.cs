using System.ComponentModel.DataAnnotations;

namespace DabaTaseApp.Models;

public partial class Category
{
    [Display(Name = "Назва категорії")]
    [Required(ErrorMessage = "Поле не повинно бути порожнім")]
    public string Name { get; set; } = null!;

    [Display(Name = "Інструктори")]
    public virtual ICollection<Instructor> Instructors { get; set; } = new List<Instructor>();
}
