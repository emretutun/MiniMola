namespace MiniMola.Application.Spotify;

public sealed record SpotifyTrackDto(
    string Id,
    string Uri,
    string Name,
    string ArtistName,
    string? AlbumName,
    string? ImageUrl,
    string? SpotifyUrl,
    int DurationMilliseconds);