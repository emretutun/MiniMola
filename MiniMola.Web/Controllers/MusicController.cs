using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiniMola.Application.Spotify;
using MiniMola.Web.Models;

namespace MiniMola.Web.Controllers;

[Authorize]
public sealed class MusicController(
    ISpotifyConnectionService spotifyConnectionService)
    : Controller
{
    private static readonly string[] RequestedScopes =
    [
        "user-read-private",
    "user-read-email",
    "streaming",
    "user-read-playback-state",
    "user-read-currently-playing",
    "user-modify-playback-state",
    "playlist-read-private",
    "user-read-playback-position"
    ];

    [HttpGet]
    public async Task<IActionResult> Index(
    CancellationToken cancellationToken)
    {
        var identityUserId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException(
                "Oturum açmış kullanıcı bulunamadı.");

        var connection =
            await spotifyConnectionService.GetStatusAsync(
                identityUserId,
                cancellationToken);

        SpotifyNowPlayingDto? nowPlaying = null;
        string? playbackError = null;

        if (connection.IsConnected)
        {
            try
            {
                nowPlaying =
                    await spotifyConnectionService
                        .GetNowPlayingAsync(
                            identityUserId,
                            cancellationToken);
            }
            catch (InvalidOperationException exception)
            {
                playbackError = exception.Message;
            }
            catch (HttpRequestException)
            {
                playbackError =
                    "Spotify servisine şu anda ulaşılamıyor.";
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                playbackError =
                    "Spotify isteği zaman aşımına uğradı.";
            }
        }

        var model = new MusicViewModel(
            connection,
            nowPlaying,
            playbackError);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Connect()
    {
        var returnUrl =
            Url.Action(nameof(Complete), "Music")
            ?? throw new InvalidOperationException(
                "Spotify dönüş adresi oluşturulamadı.");

        var authenticationProperties =
            new AuthenticationProperties
            {
                RedirectUri = returnUrl
            };

        return Challenge(
            authenticationProperties,
            "Spotify");
    }

    [HttpGet]
    public async Task<IActionResult> Complete(
        CancellationToken cancellationToken)
    {
        var externalResult =
            await HttpContext.AuthenticateAsync(
                IdentityConstants.ExternalScheme);

        if (!externalResult.Succeeded
            || externalResult.Principal is null
            || externalResult.Properties is null)
        {
            TempData["SpotifyError"] =
                "Spotify bağlantısı tamamlanamadı.";

            return RedirectToAction(nameof(Index));
        }

        try
        {
            var identityUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException(
                    "MiniMola kullanıcısı bulunamadı.");

            var spotifyAccountId =
                externalResult.Principal.FindFirstValue(
                    ClaimTypes.NameIdentifier)
                ?? externalResult.Principal.FindFirstValue(
                    "urn:spotify:user-id")
                ?? throw new InvalidOperationException(
                    "Spotify hesap kimliği alınamadı.");

            var spotifyUserId =
                externalResult.Principal.FindFirstValue(
                    "urn:spotify:user-id");

            var displayName =
                externalResult.Principal.FindFirstValue(
                    ClaimTypes.Name)
                ?? "Spotify Kullanıcısı";

            var accessToken =
                externalResult.Properties.GetTokenValue(
                    "access_token")
                ?? throw new InvalidOperationException(
                    "Spotify erişim anahtarı alınamadı.");

            var refreshToken =
                externalResult.Properties.GetTokenValue(
                    "refresh_token")
                ?? throw new InvalidOperationException(
                    "Spotify yenileme anahtarı alınamadı.");

            var expiresAt =
                ParseExpirationDate(
                    externalResult.Properties.GetTokenValue(
                        "expires_at"));

            var authorization =
                new SpotifyAuthorizationData(
                    spotifyAccountId,
                    spotifyUserId,
                    displayName,
                    null,
                    accessToken,
                    refreshToken,
                    expiresAt,
                    RequestedScopes);

            await spotifyConnectionService.SaveConnectionAsync(
                identityUserId,
                authorization,
                cancellationToken);

            TempData["SpotifySuccess"] =
                "Spotify hesabın başarıyla bağlandı.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["SpotifyError"] = exception.Message;
        }
        finally
        {
            await HttpContext.SignOutAsync(
                IdentityConstants.ExternalScheme);
        }

        return RedirectToAction(nameof(Index));
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disconnect(
        CancellationToken cancellationToken)
    {
        var identityUserId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException(
                "Oturum açmış kullanıcı bulunamadı.");

        await spotifyConnectionService.DisconnectAsync(
            identityUserId,
            cancellationToken);

        TempData["SpotifySuccess"] =
            "Spotify hesabının bağlantısı kaldırıldı.";

        return RedirectToAction(nameof(Index));
    }

    private static DateTime ParseExpirationDate(
        string? value)
    {
        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal
                | DateTimeStyles.AdjustToUniversal,
                out var expiration))
        {
            return expiration.UtcDateTime;
        }

        return DateTime.UtcNow.AddMinutes(50);
    }
}