using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DabaTaseApp.Models;

public partial class Category
{
    public int Id { get; set; }

    [Display(Name = "Назва категорії")]
    [Required(ErrorMessage = "Поле не повинно бути порожнім")]
    [StringLength(20, MinimumLength = 1, ErrorMessage = "Назва категорії має містити до 20 символів.")]
    public string Name { get; set; } = null!;

    [Display(Name = "Опис")]
    [StringLength(500, ErrorMessage = "Опис надто довгий.")]
    public string? Description { get; set; }

    [Display(Name = "Інструктори")]
    public virtual ICollection<Instructor> Instructors { get; set; } = new List<Instructor>();
}
