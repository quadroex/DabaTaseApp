using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DabaTaseApp.Models;

public partial class PracticeSession : IValidatableObject
{
    public int Id { get; set; }

    [Display(Name = "Учень")]
    public int StudentId { get; set; }

    [Display(Name = "Інструктор")]
    public int InstructorId { get; set; }

    [Display(Name = "Номер авто")]
    public string VehiclePlate { get; set; } = null!;

    [Display(Name = "Час початку")]
    public DateTime StartTime { get; set; }

    [Display(Name = "Час закінчення")]
    public DateTime EndTime { get; set; }

    [Display(Name = "Статус")]
    [Column("status")]
    public string? Status { get; set; }

    [Display(Name = "Інструктор")]
    public virtual Instructor Instructor { get; set; } = null!;

    [Display(Name = "Учень")]
    public virtual Student Student { get; set; } = null!;

    [Display(Name = "Автомобіль")]
    public virtual Vehicle VehiclePlateNavigation { get; set; } = null!;

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

//public partial class PracticeSession
//{
//    public int Id { get; set; }

//    [Display(Name = "Учень")]
//    public int StudentId { get; set; }

//    [Display(Name = "Інструктор")]
//    public int InstructorId { get; set; }

//    [Display(Name = "Номер авто")]
//    public string VehiclePlate { get; set; } = null!;

//    [Display(Name = "Час початку")]
//    public DateTime StartTime { get; set; }

//    [Display(Name = "Час закінчення")]
//    public DateTime EndTime { get; set; }

//    [Display(Name = "Статус")]
//    [Column("status")]
//    public string? Status { get; set; }

//    [Display(Name = "Інструктор")]
//    public virtual Instructor Instructor { get; set; } = null!;

//    [Display(Name = "Учень")]
//    public virtual Student Student { get; set; } = null!;

//    [Display(Name = "Автомобіль")]
//    public virtual Vehicle VehiclePlateNavigation { get; set; } = null!;
//}