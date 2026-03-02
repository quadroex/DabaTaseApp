using System;
using System.Collections.Generic;

namespace DabaTaseApp.Models;

public partial class Vehicle
{
    public string PlateNumber { get; set; } = null!;

    public string? Mark { get; set; }

    public string? Model { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<PracticeSession> PracticeSessions { get; set; } = new List<PracticeSession>();
}
