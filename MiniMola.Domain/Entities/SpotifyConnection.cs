using System;
using System.Collections.Generic;
using System.Text;
using MiniMola.Domain.Common;

namespace MiniMola.Domain.Entities;

public sealed class SpotifyConnection : BaseEntity
{
    public int UserProfileId { get; set; }

    public string SpotifyAccountId { get; set; } = string.Empty;

    public string? SpotifyUserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? ProfileImageUrl { get; set; }

    public string ProtectedAccessToken { get; set; } = string.Empty;

    public string ProtectedRefreshToken { get; set; } = string.Empty;

    public DateTime AccessTokenExpiresAtUtc { get; set; }

    public string GrantedScopes { get; set; } = string.Empty;

    public DateTime ConnectedAtUtc { get; set; } = DateTime.UtcNow;

    public UserProfile UserProfile { get; set; } = null!;
}