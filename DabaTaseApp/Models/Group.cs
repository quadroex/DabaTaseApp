using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DabaTaseApp.Models;

public partial class Group : IValidatableObject
{
    public int Id { get; set; }

    [Display(Name = "Назва групи")]
    public string GroupName { get; set; } = null!;

    [Display(Name = "Дата початку")]
    [DataType(DataType.Date)]
    public DateOnly StartDate { get; set; }

    [Display(Name = "Дата закінчення")]
    [DataType(DataType.Date)]
    public DateOnly EndDate { get; set; }

    [Display(Name = "Інструктор з теорії")]
    public int TheoryInstructorId { get; set; }

    [Display(Name = "Учні")]
    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    [Display(Name = "Інструктор з теорії")]
    public virtual Instructor TheoryInstructor { get; set; } = null!;

    [Display(Name = "Теоретичні заняття")]
    public virtual ICollection<TheorySession> TheorySessions { get; set; } = new List<TheorySession>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate < StartDate)
        {
            yield return new ValidationResult("Дата закінчення не може бути раніше за дату початку.", new[] { nameof(EndDate) });
        }
    }
}

//using System.ComponentModel.DataAnnotations;

//namespace DabaTaseApp.Models;

//public partial class Group
//{
//    public int Id { get; set; }

//    [Display(Name = "Назва групи")]
//    public string GroupName { get; set; } = null!;

//    [Display(Name = "Дата початку")]
//    [DataType(DataType.Date)]
//    public DateOnly StartDate { get; set; }

//    [Display(Name = "Дата закінчення")]
//    [DataType(DataType.Date)]
//    public DateOnly EndDate { get; set; }

//    [Display(Name = "Інструктор з теорії")]
//    public int TheoryInstructorId { get; set; }

//    [Display(Name = "Учні")]
//    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

//    [Display(Name = "Інструктор з теорії")]
//    public virtual Instructor TheoryInstructor { get; set; } = null!;

//    [Display(Name = "Теоретичні заняття")]
//    public virtual ICollection<TheorySession> TheorySessions { get; set; } = new List<TheorySession>();
//}