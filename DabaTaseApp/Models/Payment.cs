using System;
using System.Collections.Generic;

namespace DabaTaseApp.Models;

public partial class Payment
{
    public int Id { get; set; }

    public int? StudentId { get; set; }

    public decimal? Amount { get; set; }

    public DateTime? PaymentDate { get; set; }

    public virtual Student? Student { get; set; }
}
