using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.Spotify;

public interface ISpotifyConnectionService
{
    Task SaveConnectionAsync(
        string identityUserId,
        SpotifyAuthorizationData authorization,
        CancellationToken cancellationToken = default);

    Task<SpotifyConnectionStatusDto> GetStatusAsync(
        string identityUserId,
        CancellationToken cancellationToken = default);

    Task DisconnectAsync(
        string identityUserId,
        CancellationToken cancellationToken = default);
    Task<SpotifyNowPlayingDto?> GetNowPlayingAsync(
    string identityUserId,
    CancellationToken cancellationToken = default);
    Task<SpotifyAccessTokenDto> GetValidAccessTokenAsync(
    string identityUserId,
    CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpotifyTrackDto>> SearchTracksAsync(
    string identityUserId,
    string query,
    CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpotifyPlaylistDto>> GetPlaylistsAsync(
        string identityUserId,
        CancellationToken cancellationToken = default);

}