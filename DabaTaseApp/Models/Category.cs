using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DabaTaseApp.Models;

public partial class Category
{
    [Required(ErrorMessage = "The field cannot be empty.")]
    [Display(Name = "Category Name")]
    public string Name { get; set; } = null!;

    public virtual ICollection<Instructor> Instructors { get; set; } = new List<Instructor>();
}
