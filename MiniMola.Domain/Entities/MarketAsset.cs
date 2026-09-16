using System;
using System.Collections.Generic;
using System.Text;
using MiniMola.Domain.Common;
using MiniMola.Domain.Enums;

namespace MiniMola.Domain.Entities;

public sealed class MarketAsset : BaseEntity
{
    public string Symbol { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public MarketAssetType AssetType { get; set; }

    public string MarketCode { get; set; } = string.Empty;

    public string QuoteCurrency { get; set; } = string.Empty;

    public string? DataProviderCode { get; set; }

    public string? ProviderSymbol { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsActive { get; set; } = true;
}