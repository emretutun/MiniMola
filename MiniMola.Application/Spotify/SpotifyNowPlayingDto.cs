using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.Spotify;

public sealed record SpotifyNowPlayingDto(
    bool IsPlaying,
    string ContentType,
    string Title,
    string CreatorName,
    string? CollectionName,
    string? ImageUrl,
    string? SpotifyUrl,
    int ProgressMilliseconds,
    int DurationMilliseconds);