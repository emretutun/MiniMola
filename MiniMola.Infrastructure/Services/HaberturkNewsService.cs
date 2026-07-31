using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Caching.Memory;
using MiniMola.Application.News;

namespace MiniMola.Infrastructure.Services;

public sealed class HaberturkNewsService(
    HttpClient httpClient,
    IMemoryCache memoryCache)
    : INewsService
{
    private const string CacheKey =
        "news:haberturk:headlines";

    public async Task<NewsFeedDto> GetHeadlinesAsync(
        CancellationToken cancellationToken = default)
    {
        if (memoryCache.TryGetValue<NewsFeedDto>(
                CacheKey,
                out var cachedFeed)
            && cachedFeed is not null)
        {
            return cachedFeed;
        }

        using var response = await httpClient.GetAsync(
            "rss/manset.xml",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream =
            await response.Content.ReadAsStreamAsync(
                cancellationToken);

        var readerSettings = new XmlReaderSettings
        {
            Async = true,
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 2_000_000
        };

        using var xmlReader = XmlReader.Create(
            stream,
            readerSettings);

        var document = await XDocument.LoadAsync(
            xmlReader,
            LoadOptions.None,
            cancellationToken);

        var items = document
            .Descendants("item")
            .Take(10)
            .Select(CreateNewsItem)
            .Where(item => item is not null)
            .Cast<NewsItemDto>()
            .ToList();

        if (items.Count == 0)
        {
            throw new InvalidOperationException(
                "Habertürk RSS akışında haber bulunamadı.");
        }

        var feed = new NewsFeedDto(
            "Habertürk",
            DateTimeOffset.UtcNow,
            items);

        memoryCache.Set(
            CacheKey,
            feed,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow =
                    TimeSpan.FromMinutes(5)
            });

        return feed;
    }

    private static NewsItemDto? CreateNewsItem(
        XElement element)
    {
        var title = GetElementValue(element, "title");
        var link = GetElementValue(element, "link");
        var description =
            GetElementValue(element, "description");
        var publicationDate =
            GetElementValue(element, "pubDate");

        if (string.IsNullOrWhiteSpace(title)
            || !TryGetSafeHaberturkUrl(
                link,
                out var safeUrl))
        {
            return null;
        }

        DateTimeOffset? publishedAt = null;

        if (DateTimeOffset.TryParse(
                publicationDate,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var parsedDate))
        {
            publishedAt = parsedDate;
        }

        return new NewsItemDto(
            CreateStableId(safeUrl),
            CleanText(title, 160),
            CleanText(description, 240),
            safeUrl,
            publishedAt);
    }

    private static string GetElementValue(
        XElement element,
        string elementName)
    {
        return element
            .Elements()
            .FirstOrDefault(
                child => child.Name.LocalName == elementName)
            ?.Value
            ?.Trim()
            ?? string.Empty;
    }

    private static bool TryGetSafeHaberturkUrl(
        string value,
        out string safeUrl)
    {
        safeUrl = string.Empty;

        if (!Uri.TryCreate(
                value,
                UriKind.Absolute,
                out var uri))
        {
            return false;
        }

        var hasAllowedScheme =
            uri.Scheme == Uri.UriSchemeHttps
            || uri.Scheme == Uri.UriSchemeHttp;

        var isHaberturkDomain =
            uri.Host.Equals(
                "haberturk.com",
                StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith(
                ".haberturk.com",
                StringComparison.OrdinalIgnoreCase);

        if (!hasAllowedScheme || !isHaberturkDomain)
        {
            return false;
        }

        safeUrl = uri.AbsoluteUri;
        return true;
    }

    private static string CleanText(
        string value,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var withoutHtml = Regex.Replace(
            value,
            "<[^>]+>",
            " ");

        var decoded = WebUtility.HtmlDecode(withoutHtml);

        var normalized = Regex.Replace(
                decoded,
                @"\s+",
                " ")
            .Trim();

        if (normalized.Length <= maximumLength)
        {
            return normalized;
        }

        return normalized[..maximumLength].TrimEnd()
               + "…";
    }

    private static string CreateStableId(string url)
    {
        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(url));

        return Convert
            .ToHexString(bytes)
            .ToLowerInvariant()[..16];
    }
}