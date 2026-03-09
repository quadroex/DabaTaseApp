using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DabaTaseApp.Models;

public partial class TheorySession : IValidatableObject
{
    [Display(Name = "Час початку")]
    public DateTime StartTime { get; set; }

    [Display(Name = "Інструктор")]
    public int InstructorId { get; set; }

    [Display(Name = "Група")]
    public int GroupId { get; set; }

    [Display(Name = "Аудиторія")]
    public string Location { get; set; } = null!;

    [Display(Name = "Час закінчення")]
    public DateTime EndTime { get; set; }

    [Display(Name = "Статус")]
    [Column("status")]
    public string? Status { get; set; }

    [Display(Name = "Група")]
    public virtual Group Group { get; set; } = null!;

    [Display(Name = "Інструктор")]
    public virtual Instructor Instructor { get; set; } = null!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTime <= StartTime)
        {
            yield return new ValidationResult("Час закінчення повинен бути пізніше за час початку.", new[] { nameof(EndTime) });
        }
    }
}

//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;

//namespace DabaTaseApp.Models;

//public partial class TheorySession
//{
//    [Display(Name = "Час початку")]
//    public DateTime StartTime { get; set; }

//    [Display(Name = "Інструктор")]
//    public int InstructorId { get; set; }

//    [Display(Name = "Група")]
//    public int GroupId { get; set; }

//    [Display(Name = "Аудиторія")]
//    public string Location { get; set; } = null!;

//    [Display(Name = "Час закінчення")]
//    public DateTime EndTime { get; set; }

//    [Display(Name = "Статус")]
//    [Column("status")]
//    public string? Status { get; set; }

//    [Display(Name = "Група")]
//    public virtual Group Group { get; set; } = null!;

//    [Display(Name = "Інструктор")]
//    public virtual Instructor Instructor { get; set; } = null!;
//}