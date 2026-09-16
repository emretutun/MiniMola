using System;
using System.Collections.Generic;
using System.Text;
using MiniMola.Domain.Common;

namespace MiniMola.Domain.Entities;

public sealed class UserFavoriteAsset : BaseEntity
{
    public int UserProfileId { get; set; }

    public int MarketAssetId { get; set; }

    public int SortOrder { get; set; }

    public UserProfile UserProfile { get; set; } = null!;

    public MarketAsset MarketAsset { get; set; } = null!;
}