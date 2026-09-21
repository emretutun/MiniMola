using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MiniMola.Infrastructure.Services;

// KAP identity and attachment validation is independent of the PDF layout reader.
public static class KapPortfolioDiscovery
{
    public const string FundOid = "4028328c950ba8c70195140f682921da";
    public const string Subject = "Portföy Dağılım Raporu";

    public static long? FindLatest(string json, DateTime nowUtc, string fundCode = "THF")
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.EnumerateArray()
            .Where(x => x.GetProperty("fundCode").GetString() == fundCode
                && x.GetProperty("subject").GetString() == Subject)
            .Select(x => new
            {
                Id = x.GetProperty("disclosureIndex").GetInt64(),
                Published = ParseDate(x.GetProperty("publishDate").GetString()!, "dd.MM.yyyy HH:mm:ss")
            })
            .Where(x => x.Id > 0 && x.Published <= nowUtc)
            .OrderByDescending(x => x.Published).ThenByDescending(x => x.Id)
            .Select(x => (long?)x.Id).FirstOrDefault();
    }

    public static KapPortfolioReportDefinition ReadDetail(string json, long expectedId, DateTime nowUtc,
        string fundCode = "THF", string fundOid = FundOid)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.GetArrayLength() != 1) throw new InvalidDataException("Beklenmeyen KAP bildirim yanıtı.");
        var item = root[0];
        var disclosure = item.GetProperty("disclosure");
        var basic = disclosure.GetProperty("disclosureBasic");
        if (basic.GetProperty("stockCode").GetString() != fundCode
            || basic.GetProperty("title").GetString() != Subject
            || basic.GetProperty("disclosureIndex").GetInt64() != expectedId
            || basic.GetProperty("isBlocked").GetBoolean()
            || !string.Equals(disclosure.GetProperty("disclosureDetail").GetProperty("fundOid").GetString(), fundOid, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("KAP fon/bildirim kimliği doğrulanamadı.");

        var year = basic.GetProperty("year").GetInt32();
        var month = basic.GetProperty("donem").GetInt32();
        var date = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        var published = ParseDate(basic.GetProperty("publishDate").GetString()!, "yyyy.MM.dd HH:mm:ss");
        if (published > nowUtc || date >= DateOnly.FromDateTime(published.AddHours(3)))
            throw new InvalidDataException("KAP rapor dönemi doğrulanamadı.");

        var expectedName = $"{fundCode}_{year}.{month:00}.pdf";
        var attachments = item.GetProperty("attachments").EnumerateArray()
            .Where(x => (string.Equals(x.GetProperty("fileName").GetString(), expectedName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(x.GetProperty("fileName").GetString(), $"{fundCode}.pdf", StringComparison.OrdinalIgnoreCase))
                && string.Equals(x.GetProperty("fileExtension").GetString(), "pdf", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (attachments.Length != 1) throw new InvalidDataException("Rapor eki adı/formatı henüz desteklenmiyor veya birden fazla ek var.");
        var id = attachments[0].GetProperty("objId").GetString()!;
        if (!Regex.IsMatch(id, "\\A[a-fA-F0-9]{32}\\z")) throw new InvalidDataException("Geçersiz KAP dosya kimliği.");
        return new(date, published, expectedId, id, attachments[0].GetProperty("fileName").GetString()!);
    }

    public static bool HasValidHoldings(IReadOnlyList<ParsedFundHolding> holdings) =>
        holdings.Count >= 5 && holdings.All(x => x.WeightPercent > 0 && x.WeightPercent <= 100)
        && holdings.Select(x => x.Symbol).Distinct(StringComparer.OrdinalIgnoreCase).Count() == holdings.Count
        && holdings.Sum(x => x.WeightPercent) is >= 80 and <= 100.5m;

    private static DateTime ParseDate(string text, string format) =>
        DateTime.SpecifyKind(DateTime.ParseExact(text, format, CultureInfo.InvariantCulture).AddHours(-3), DateTimeKind.Utc);

    public static IReadOnlyDictionary<string, KapEquityFund> ReadEquityCatalog(string html)
    {
        var result = new Dictionary<string, KapEquityFund>(StringComparer.OrdinalIgnoreCase);
        // The public YF page embeds flat catalog records in the Next.js payload.
        var decoded = html.Replace("\\\"", "\"");
        foreach (Match match in Regex.Matches(decoded, "\\{[^{}]*\"fundClass\":\"HS\"[^{}]*\\}",
                     RegexOptions.None, TimeSpan.FromSeconds(2)))
        {
            using var document = JsonDocument.Parse(match.Value);
            var item = document.RootElement;
            if (item.GetProperty("fundType").GetString() != "YF"
                || item.GetProperty("fundState").GetString() != "Y") continue;
            var code = item.GetProperty("fundCode").GetString()!;
            var oid = item.GetProperty("fundOid").GetString()!;
            var name = item.GetProperty("fundName").GetString()!;
            var upper = name.ToUpperInvariant();
            if (!Regex.IsMatch(code, "\\A[A-Z0-9]{2,8}\\z")
                || !Regex.IsMatch(oid, "\\A[a-fA-F0-9]{32}\\z")
                || upper.Contains("YABANCI") || upper.Contains("ARBITRAJ") || upper.Contains("ARBİTRAJ")
                || upper.Contains("SERBEST") || upper.Contains("FON SEPET")) continue;
            if (result.TryGetValue(code, out var previous) && previous.Oid != oid)
                throw new InvalidDataException("KAP kataloğunda çakışan fon kimliği.");
            result[code] = new(code, oid, name);
        }
        if (result.Count == 0) throw new InvalidDataException("KAP hisse fonu kataloğu okunamadı.");
        return result;
    }
}

public sealed record KapEquityFund(string Code, string Oid, string Name);

public sealed record KapPortfolioReportDefinition(DateOnly ReportDate, DateTime PublishedAtUtc,
    long KapNotificationId, string DocumentObjectId, string FileName)
{
    public string DocumentPath => $"tr/api/file/download/{DocumentObjectId}";
    public string DocumentUrl => $"https://www.kap.org.tr/{DocumentPath}";
    public string NotificationUrl => $"https://www.kap.org.tr/tr/Bildirim/{KapNotificationId}";
}
