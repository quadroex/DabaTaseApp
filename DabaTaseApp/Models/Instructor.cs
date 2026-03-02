using System;
using System.Collections.Generic;

namespace DabaTaseApp.Models;

public partial class Instructor
{
    public int Id { get; set; }

    public string? FullName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? LicenseSerial { get; set; }

    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

    public virtual ICollection<PracticeSession> PracticeSessions { get; set; } = new List<PracticeSession>();

    public virtual ICollection<TheorySession> TheorySessions { get; set; } = new List<TheorySession>();

    public virtual ICollection<Category> CategoryNames { get; set; } = new List<Category>();
}
