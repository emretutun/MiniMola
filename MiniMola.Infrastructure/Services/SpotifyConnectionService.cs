using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniMola.Application.Spotify;
using MiniMola.Domain.Entities;
using MiniMola.Infrastructure.Persistence;
using MiniMola.Infrastructure.Spotify;

namespace MiniMola.Infrastructure.Services;

public sealed class SpotifyConnectionService(
    ApplicationDbContext dbContext,
    IDataProtectionProvider dataProtectionProvider,
    IHttpClientFactory httpClientFactory,
    IOptions<SpotifyOptions> spotifyOptions)
    : ISpotifyConnectionService
{
    private readonly IDataProtector tokenProtector =
        dataProtectionProvider.CreateProtector(
            "MiniMola.SpotifyConnectionTokens.v1");

    private readonly SpotifyOptions options =
        spotifyOptions.Value;

    public async Task SaveConnectionAsync(
        string identityUserId,
        SpotifyAuthorizationData authorization,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identityUserId);
        ArgumentNullException.ThrowIfNull(authorization);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            authorization.SpotifyAccountId);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            authorization.AccessToken);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            authorization.RefreshToken);

        var userProfileId = await dbContext.UserProfiles
            .AsNoTracking()
            .Where(x => x.IdentityUserId == identityUserId)
            .Select(x => (int?)x.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                "MiniMola kullanıcı profili bulunamadı.");

        var isLinkedToAnotherUser =
            await dbContext.SpotifyConnections
                .AsNoTracking()
                .AnyAsync(
                    x => x.SpotifyAccountId
                             == authorization.SpotifyAccountId
                         && x.UserProfileId != userProfileId,
                    cancellationToken);

        if (isLinkedToAnotherUser)
        {
            throw new InvalidOperationException(
                "Bu Spotify hesabı başka bir "
                + "MiniMola kullanıcısına bağlı.");
        }

        var connection = await dbContext.SpotifyConnections
            .SingleOrDefaultAsync(
                x => x.UserProfileId == userProfileId,
                cancellationToken);

        var now = DateTime.UtcNow;

        var displayName =
            string.IsNullOrWhiteSpace(authorization.DisplayName)
                ? "Spotify Kullanıcısı"
                : authorization.DisplayName.Trim();

        if (displayName.Length > 150)
        {
            displayName = displayName[..150];
        }

        var grantedScopes = string.Join(
            ' ',
            authorization.GrantedScopes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal));

        if (connection is null)
        {
            connection = new SpotifyConnection
            {
                UserProfileId = userProfileId,
                ConnectedAtUtc = now
            };

            dbContext.SpotifyConnections.Add(connection);
        }

        connection.SpotifyAccountId =
            authorization.SpotifyAccountId.Trim();

        connection.SpotifyUserId =
            NormalizeOptionalValue(
                authorization.SpotifyUserId,
                200);

        connection.DisplayName = displayName;

        connection.ProfileImageUrl =
            NormalizeOptionalValue(
                authorization.ProfileImageUrl,
                1000);

        connection.ProtectedAccessToken =
            tokenProtector.Protect(
                authorization.AccessToken);

        connection.ProtectedRefreshToken =
            tokenProtector.Protect(
                authorization.RefreshToken);

        connection.AccessTokenExpiresAtUtc =
            EnsureUtc(authorization.AccessTokenExpiresAtUtc);

        connection.GrantedScopes = grantedScopes;
        connection.UpdatedAtUtc = now;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<SpotifyConnectionStatusDto> GetStatusAsync(
        string identityUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identityUserId);

        var status = await dbContext.SpotifyConnections
            .AsNoTracking()
            .Where(
                x => x.UserProfile.IdentityUserId
                     == identityUserId)
            .Select(
                x => new SpotifyConnectionStatusDto(
                    true,
                    x.DisplayName,
                    x.ProfileImageUrl,
                    x.ConnectedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

        return status
               ?? new SpotifyConnectionStatusDto(
                   false,
                   null,
                   null,
                   null);
    }

    public async Task<SpotifyNowPlayingDto?> GetNowPlayingAsync(
        string identityUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identityUserId);

        var token =
            await GetValidAccessTokenAsync(
                identityUserId,
                cancellationToken);

        var client =
            httpClientFactory.CreateClient("SpotifyApi");

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "me/player/currently-playing"
            + "?additional_types=track,episode");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token.AccessToken);

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException(
                "Spotify yetkilendirmesi geçersiz. "
                + "Hesabını yeniden bağlaman gerekiyor.");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException(
                "Spotify bu oynatma bilgisine "
                + "erişim izni vermedi.");
        }

        if (response.StatusCode
            == HttpStatusCode.TooManyRequests)
        {
            throw new InvalidOperationException(
                "Spotify istek sınırına ulaşıldı. "
                + "Biraz sonra tekrar dene.");
        }

        response.EnsureSuccessStatusCode();

        await using var responseStream =
            await response.Content.ReadAsStreamAsync(
                cancellationToken);

        using var document =
            await JsonDocument.ParseAsync(
                responseStream,
                cancellationToken: cancellationToken);

        var root = document.RootElement;

        if (!root.TryGetProperty(
                "item",
                out var item)
            || item.ValueKind is JsonValueKind.Null
                or JsonValueKind.Undefined)
        {
            return null;
        }

        var contentType =
            GetString(item, "type") ?? "unknown";

        var title =
            GetString(item, "name")
            ?? "Bilinmeyen içerik";

        string creatorName;
        string? collectionName;
        string? imageUrl;

        if (contentType == "episode")
        {
            creatorName =
                GetNestedString(item, "show", "name")
                ?? "Podcast";

            collectionName = "Podcast";

            imageUrl =
                GetFirstImageUrl(item)
                ?? GetNestedFirstImageUrl(
                    item,
                    "show");
        }
        else
        {
            creatorName =
                GetArtistNames(item);

            collectionName =
                GetNestedString(
                    item,
                    "album",
                    "name");

            imageUrl =
                GetNestedFirstImageUrl(
                    item,
                    "album");
        }

        var spotifyUrl =
            GetNestedString(
                item,
                "external_urls",
                "spotify");

        var isPlaying =
            root.TryGetProperty(
                "is_playing",
                out var isPlayingElement)
            && isPlayingElement.ValueKind
                is JsonValueKind.True;

        var progressMilliseconds =
            GetInteger(root, "progress_ms");

        var durationMilliseconds =
            GetInteger(item, "duration_ms");

        return new SpotifyNowPlayingDto(
            isPlaying,
            contentType,
            title,
            creatorName,
            collectionName,
            imageUrl,
            spotifyUrl,
            progressMilliseconds,
            durationMilliseconds);
    }

    public async Task<SpotifyAccessTokenDto>
        GetValidAccessTokenAsync(
            string identityUserId,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identityUserId);

        var connection = await dbContext.SpotifyConnections
            .Where(
                x => x.UserProfile.IdentityUserId
                     == identityUserId)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                "Spotify hesabı bağlı değil.");

        var expiresAt =
            EnsureUtc(connection.AccessTokenExpiresAtUtc);

        if (expiresAt > DateTime.UtcNow.AddMinutes(2))
        {
            return new SpotifyAccessTokenDto(
                UnprotectToken(
                    connection.ProtectedAccessToken),
                expiresAt);
        }

        return await RefreshAccessTokenAsync(
            connection,
            cancellationToken);
    }
    public async Task<IReadOnlyList<SpotifyTrackDto>>
    SearchTracksAsync(
        string identityUserId,
        string query,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identityUserId);

        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var normalizedQuery = query.Trim();

        if (normalizedQuery.Length > 100)
        {
            normalizedQuery = normalizedQuery[..100];
        }

        var token =
            await GetValidAccessTokenAsync(
                identityUserId,
                cancellationToken);

        var requestUrl =
            "search?type=track&limit=10&q="
            + Uri.EscapeDataString(normalizedQuery);

        var client =
            httpClientFactory.CreateClient("SpotifyApi");

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            requestUrl);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token.AccessToken);

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException(
                "Spotify yetkilendirmesi geçersiz.");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException(
                "Spotify arama izni vermedi.");
        }

        if (response.StatusCode
            == HttpStatusCode.TooManyRequests)
        {
            throw new InvalidOperationException(
                "Spotify arama sınırına ulaşıldı. "
                + "Biraz sonra tekrar dene.");
        }

        response.EnsureSuccessStatusCode();

        await using var responseStream =
            await response.Content.ReadAsStreamAsync(
                cancellationToken);

        using var document =
            await JsonDocument.ParseAsync(
                responseStream,
                cancellationToken: cancellationToken);

        var root = document.RootElement;

        if (!root.TryGetProperty(
                "tracks",
                out var tracksContainer)
            || tracksContainer.ValueKind
                != JsonValueKind.Object
            || !tracksContainer.TryGetProperty(
                "items",
                out var items)
            || items.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var tracks = new List<SpotifyTrackDto>();

        foreach (var item in items.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var id = GetString(item, "id");
            var uri = GetString(item, "uri");
            var name = GetString(item, "name");

            if (string.IsNullOrWhiteSpace(id)
                || string.IsNullOrWhiteSpace(uri)
                || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            tracks.Add(
                new SpotifyTrackDto(
                    id,
                    uri,
                    name,
                    GetArtistNames(item),
                    GetNestedString(
                        item,
                        "album",
                        "name"),
                    GetNestedFirstImageUrl(
                        item,
                        "album"),
                    GetNestedString(
                        item,
                        "external_urls",
                        "spotify"),
                    GetInteger(
                        item,
                        "duration_ms")));
        }

        return tracks;
    }

    public async Task<IReadOnlyList<SpotifyPlaylistDto>>
        GetPlaylistsAsync(
            string identityUserId,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identityUserId);

        var token =
            await GetValidAccessTokenAsync(
                identityUserId,
                cancellationToken);

        var client =
            httpClientFactory.CreateClient("SpotifyApi");

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "me/playlists?limit=20&offset=0");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token.AccessToken);

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException(
                "Spotify yetkilendirmesi geçersiz.");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException(
                "Spotify playlist bilgilerine erişim izni vermedi.");
        }

        if (response.StatusCode
            == HttpStatusCode.TooManyRequests)
        {
            throw new InvalidOperationException(
                "Spotify istek sınırına ulaşıldı. "
                + "Biraz sonra tekrar dene.");
        }

        response.EnsureSuccessStatusCode();

        await using var responseStream =
            await response.Content.ReadAsStreamAsync(
                cancellationToken);

        using var document =
            await JsonDocument.ParseAsync(
                responseStream,
                cancellationToken: cancellationToken);

        var root = document.RootElement;

        if (!root.TryGetProperty(
                "items",
                out var items)
            || items.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var playlists = new List<SpotifyPlaylistDto>();

        foreach (var item in items.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var id = GetString(item, "id");
            var uri = GetString(item, "uri");
            var name = GetString(item, "name");

            if (string.IsNullOrWhiteSpace(id)
                || string.IsNullOrWhiteSpace(uri)
                || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var totalItems =
                GetNestedInteger(
                    item,
                    "items",
                    "total");

            if (totalItems == 0)
            {
                totalItems =
                    GetNestedInteger(
                        item,
                        "tracks",
                        "total");
            }

            playlists.Add(
                new SpotifyPlaylistDto(
                    id,
                    uri,
                    name,
                    GetString(item, "description"),
                    GetFirstImageUrl(item),
                    GetNestedString(
                        item,
                        "external_urls",
                        "spotify"),
                    GetNestedString(
                        item,
                        "owner",
                        "display_name"),
                    totalItems));
        }

        return playlists;
    }

    public async Task DisconnectAsync(
        string identityUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identityUserId);

        var connection = await dbContext.SpotifyConnections
            .SingleOrDefaultAsync(
                x => x.UserProfile.IdentityUserId
                     == identityUserId,
                cancellationToken);

        if (connection is null)
        {
            return;
        }

        dbContext.SpotifyConnections.Remove(connection);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<SpotifyAccessTokenDto>
        RefreshAccessTokenAsync(
            SpotifyConnection connection,
            CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.ClientId)
            || string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            throw new InvalidOperationException(
                "Spotify uygulama bilgileri bulunamadı.");
        }

        var refreshToken =
            UnprotectToken(
                connection.ProtectedRefreshToken);

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(
                $"{options.ClientId}:{options.ClientSecret}"));

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://accounts.spotify.com/api/token");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Basic",
                credentials);

        request.Content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            });

        var client =
            httpClientFactory.CreateClient("SpotifyApi");

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        var responseJson =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            if (responseJson.Contains(
                    "invalid_grant",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Spotify bağlantısının süresi dolmuş. "
                    + "Hesabını yeniden bağlaman gerekiyor.");
            }

            throw new HttpRequestException(
                "Spotify access token yenilenemedi. "
                + $"Durum kodu: {(int)response.StatusCode}.");
        }

        using var document =
            JsonDocument.Parse(responseJson);

        var root = document.RootElement;

        var accessToken =
            GetString(root, "access_token")
            ?? throw new InvalidOperationException(
                "Spotify yeni access token döndürmedi.");

        var expiresInSeconds =
            GetInteger(root, "expires_in");

        if (expiresInSeconds <= 0)
        {
            expiresInSeconds = 3600;
        }

        var newRefreshToken =
            GetString(root, "refresh_token");

        var grantedScopes =
            GetString(root, "scope");

        var now = DateTime.UtcNow;
        var expiresAt =
            now.AddSeconds(expiresInSeconds);

        connection.ProtectedAccessToken =
            tokenProtector.Protect(accessToken);

        if (!string.IsNullOrWhiteSpace(newRefreshToken))
        {
            connection.ProtectedRefreshToken =
                tokenProtector.Protect(newRefreshToken);
        }

        if (!string.IsNullOrWhiteSpace(grantedScopes)
            && grantedScopes.Length <= 1000)
        {
            connection.GrantedScopes = grantedScopes;
        }

        connection.AccessTokenExpiresAtUtc = expiresAt;
        connection.UpdatedAtUtc = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new SpotifyAccessTokenDto(
            accessToken,
            expiresAt);
    }

    private string UnprotectToken(
        string protectedToken)
    {
        try
        {
            return tokenProtector.Unprotect(
                protectedToken);
        }
        catch (CryptographicException)
        {
            throw new InvalidOperationException(
                "Spotify bağlantı anahtarları okunamadı. "
                + "Hesabını yeniden bağlaman gerekiyor.");
        }
    }

    private static string? GetString(
        JsonElement element,
        string propertyName)
    {
        return element.TryGetProperty(
                   propertyName,
                   out var property)
               && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static string? GetNestedString(
        JsonElement element,
        string objectName,
        string propertyName)
    {
        return element.TryGetProperty(
                   objectName,
                   out var nestedObject)
               && nestedObject.ValueKind
                   == JsonValueKind.Object
            ? GetString(
                nestedObject,
                propertyName)
            : null;
    }

    private static int GetInteger(
        JsonElement element,
        string propertyName)
    {
        return element.TryGetProperty(
                   propertyName,
                   out var property)
               && property.TryGetInt32(out var value)
            ? value
            : 0;
    }
    private static int GetNestedInteger(
    JsonElement element,
    string objectName,
    string propertyName)
    {
        return element.TryGetProperty(
                   objectName,
                   out var nestedObject)
               && nestedObject.ValueKind
                   == JsonValueKind.Object
            ? GetInteger(
                nestedObject,
                propertyName)
            : 0;
    }

    private static string GetArtistNames(
        JsonElement item)
    {
        if (!item.TryGetProperty(
                "artists",
                out var artists)
            || artists.ValueKind
                != JsonValueKind.Array)
        {
            return "Bilinmeyen sanatçı";
        }

        var names = artists
            .EnumerateArray()
            .Select(x => GetString(x, "name"))
            .Where(x => !string.IsNullOrWhiteSpace(x));

        var result = string.Join(", ", names);

        return string.IsNullOrWhiteSpace(result)
            ? "Bilinmeyen sanatçı"
            : result;
    }

    private static string? GetFirstImageUrl(
        JsonElement element)
    {
        if (!element.TryGetProperty(
                "images",
                out var images)
            || images.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var image in images.EnumerateArray())
        {
            var url = GetString(image, "url");

            if (!string.IsNullOrWhiteSpace(url))
            {
                return url;
            }
        }

        return null;
    }

    private static string? GetNestedFirstImageUrl(
        JsonElement element,
        string objectName)
    {
        return element.TryGetProperty(
                   objectName,
                   out var nestedObject)
               && nestedObject.ValueKind
                   == JsonValueKind.Object
            ? GetFirstImageUrl(nestedObject)
            : null;
    }

    private static string? NormalizeOptionalValue(
        string? value,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        return normalized.Length <= maximumLength
            ? normalized
            : normalized[..maximumLength];
    }

    private static DateTime EnsureUtc(
        DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(
                value,
                DateTimeKind.Utc)
        };
    }
}