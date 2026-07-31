using MiniMola.Application.Spotify;

namespace MiniMola.Web.Models;

public sealed record MusicViewModel(
    SpotifyConnectionStatusDto Connection,
    SpotifyNowPlayingDto? NowPlaying,
    string? PlaybackError);