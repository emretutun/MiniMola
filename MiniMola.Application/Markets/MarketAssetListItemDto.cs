using System;
using System.Collections.Generic;
using System.Text;
using MiniMola.Domain.Enums;

namespace MiniMola.Application.Markets;

public sealed record MarketAssetListItemDto(
    int Id,
    string Symbol,
    string Name,
    MarketAssetType AssetType,
    string MarketCode,
    string QuoteCurrency,
    decimal? Price,
    decimal? DailyChangePercent,
    MarketPriceKind? PriceKind,
    string? Source,
    DateTime? ObservedAtUtc,
    bool IsFavorite);