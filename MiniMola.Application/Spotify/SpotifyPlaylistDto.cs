namespace MiniMola.Application.Spotify;

public sealed record SpotifyPlaylistDto(
    string Id,
    string Uri,
    string Name,
    string? Description,
    string? ImageUrl,
    string? SpotifyUrl,
    string? OwnerName,
    int TotalItems);