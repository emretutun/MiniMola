using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.Spotify;

public sealed record SpotifyAuthorizationData(
    string SpotifyAccountId,
    string? SpotifyUserId,
    string DisplayName,
    string? ProfileImageUrl,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    IReadOnlyCollection<string> GrantedScopes);