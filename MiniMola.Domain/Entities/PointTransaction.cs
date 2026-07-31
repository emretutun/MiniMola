using System;
using System.Collections.Generic;
using System.Text;
using MiniMola.Domain.Common;
using MiniMola.Domain.Enums;

namespace MiniMola.Domain.Entities;

public sealed class PointTransaction : BaseEntity
{
    public int UserProfileId { get; set; }

    public PointTransactionType TransactionType { get; set; }

    public int Amount { get; set; }

    public int BalanceAfter { get; set; }

    public string ReferenceId { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public UserProfile UserProfile { get; set; } = null!;
}