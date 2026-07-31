using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniMola.Application.Spotify;

namespace MiniMola.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/spotify")]
public sealed class SpotifyApiController(
    ISpotifyConnectionService spotifyConnectionService)
    : ControllerBase
{
    [HttpGet("token")]
    [ResponseCache(
        NoStore = true,
        Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<SpotifyAccessTokenDto>>
        GetAccessToken(
            CancellationToken cancellationToken)
    {
        DisableResponseCaching();

        var identityUserId = GetIdentityUserId();

        if (identityUserId is null)
        {
            return Unauthorized();
        }

        try
        {
            var token =
                await spotifyConnectionService
                    .GetValidAccessTokenAsync(
                        identityUserId,
                        cancellationToken);

            return Ok(token);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(
                new
                {
                    message = exception.Message
                });
        }
        catch (HttpRequestException)
        {
            return SpotifyUnavailable();
        }
    }

    [HttpGet("search")]
    [ResponseCache(
        NoStore = true,
        Location = ResponseCacheLocation.None)]
    public async Task<
        ActionResult<IReadOnlyList<SpotifyTrackDto>>>
        SearchTracks(
            [FromQuery] string query,
            CancellationToken cancellationToken)
    {
        DisableResponseCaching();

        var identityUserId = GetIdentityUserId();

        if (identityUserId is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(query)
            || query.Trim().Length < 2)
        {
            return BadRequest(
                new
                {
                    message =
                        "Arama için en az 2 karakter gir."
                });
        }

        try
        {
            var tracks =
                await spotifyConnectionService
                    .SearchTracksAsync(
                        identityUserId,
                        query,
                        cancellationToken);

            return Ok(tracks);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(
                new
                {
                    message = exception.Message
                });
        }
        catch (HttpRequestException)
        {
            return SpotifyUnavailable();
        }
    }

    [HttpGet("playlists")]
    [ResponseCache(
        NoStore = true,
        Location = ResponseCacheLocation.None)]
    public async Task<
        ActionResult<IReadOnlyList<SpotifyPlaylistDto>>>
        GetPlaylists(
            CancellationToken cancellationToken)
    {
        DisableResponseCaching();

        var identityUserId = GetIdentityUserId();

        if (identityUserId is null)
        {
            return Unauthorized();
        }

        try
        {
            var playlists =
                await spotifyConnectionService
                    .GetPlaylistsAsync(
                        identityUserId,
                        cancellationToken);

            return Ok(playlists);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(
                new
                {
                    message = exception.Message
                });
        }
        catch (HttpRequestException)
        {
            return SpotifyUnavailable();
        }
    }

    private string? GetIdentityUserId()
    {
        return User.FindFirstValue(
            ClaimTypes.NameIdentifier);
    }

    private void DisableResponseCaching()
    {
        Response.Headers.CacheControl =
            "no-store, no-cache, must-revalidate";

        Response.Headers.Pragma = "no-cache";
    }

    private ObjectResult SpotifyUnavailable()
    {
        return StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            new
            {
                message =
                    "Spotify servisine şu anda ulaşılamıyor."
            });
    }
}