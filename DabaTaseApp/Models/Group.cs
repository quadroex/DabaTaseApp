using System;
using System.Collections.Generic;

namespace DabaTaseApp.Models;

public partial class Group
{
    public int Id { get; set; }

    public string? GroupName { get; set; }

    public int? TheoryInstructorId { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual Instructor? TheoryInstructor { get; set; }

    public virtual ICollection<TheorySession> TheorySessions { get; set; } = new List<TheorySession>();
}
