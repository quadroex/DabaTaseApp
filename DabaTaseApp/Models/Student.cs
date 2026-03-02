using System;
using System.Collections.Generic;

namespace DabaTaseApp.Models;

public partial class Student
{
    public int Id { get; set; }

    public string? FullName { get; set; }

    public int? GroupId { get; set; }

    public int? Balance { get; set; }

    public string? TargetCategory { get; set; }

    public virtual Group? Group { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<PracticeSession> PracticeSessions { get; set; } = new List<PracticeSession>();
}
