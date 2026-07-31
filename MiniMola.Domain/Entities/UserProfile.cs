using System;
using System.Collections.Generic;
using System.Text;

using MiniMola.Domain.Common;

namespace MiniMola.Domain.Entities;

public sealed class UserProfile : BaseEntity
{
    public string IdentityUserId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public int PointBalance { get; set; }
}