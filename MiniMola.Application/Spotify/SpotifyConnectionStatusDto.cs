using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.Spotify;

public sealed record SpotifyConnectionStatusDto(
    bool IsConnected,
    string? DisplayName,
    string? ProfileImageUrl,
    DateTime? ConnectedAtUtc);