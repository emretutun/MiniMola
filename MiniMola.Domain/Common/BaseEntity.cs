using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Domain.Common;

public abstract class BaseEntity
{
    public int Id { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}