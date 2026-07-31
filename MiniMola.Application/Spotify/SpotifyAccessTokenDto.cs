using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.Spotify;

public sealed record SpotifyAccessTokenDto(
    string AccessToken,
    DateTime ExpiresAtUtc);