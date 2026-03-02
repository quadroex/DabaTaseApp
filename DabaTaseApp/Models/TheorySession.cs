using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DabaTaseApp.Models;

public partial class TheorySession
{
    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string? Location { get; set; }

    [Column("status")]
    public SessionStatus? Status { get; set; }

    public int InstructorId { get; set; }

    public int? GroupId { get; set; }

    public virtual Group? Group { get; set; }

    public virtual Instructor Instructor { get; set; } = null!;
}
