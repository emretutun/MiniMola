using System;
using System.Collections.Generic;
using System.Text;
using MiniMola.Domain.Common;
using MiniMola.Domain.Enums;

namespace MiniMola.Domain.Entities;

public sealed class MarketPriceSnapshot : BaseEntity
{
    public int MarketAssetId { get; set; }

    public decimal Price { get; set; }

    public decimal? DailyChangePercent { get; set; }

    public MarketPriceKind PriceKind { get; set; }

    public string Source { get; set; } = string.Empty;

    public DateTime ObservedAtUtc { get; set; }

    public MarketAsset MarketAsset { get; set; } = null!;
}