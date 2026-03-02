using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DabaTaseApp.Models;

public partial class PracticeSession
{
    public int Id { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    [Column("status")]
    public SessionStatus? Status { get; set; }

    public int? InstructorId { get; set; }

    public int? StudentId { get; set; }

    public string? VehiclePlate { get; set; }

    public virtual Instructor? Instructor { get; set; }

    public virtual Student? Student { get; set; }

    public virtual Vehicle? VehiclePlateNavigation { get; set; }
}
