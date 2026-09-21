using System.Globalization;
using System.Text.RegularExpressions;

namespace MiniMola.Infrastructure.Services;

public sealed partial class KapFundPortfolioPdfParser
{
    // Displayed percentages use portfolio value, not NAV. Reconcile amounts first.
    public static IReadOnlyList<ParsedFundHolding> ParseYapiKrediText(
        IReadOnlyList<string> pages, string fundCode, DateOnly reportDate)
    {
        const string number = @"(?:\d{1,3}(?:,\d{3})+|\d+)\.\d{2}";
        var first = pages.FirstOrDefault() ?? "";
        var culture = CultureInfo.GetCultureInfo("tr-TR");
        var month = reportDate.ToString("MMMM yyyy", culture).ToUpper(culture);
        var normalized = Regex.Replace(first, @"\s+", " ");
        if (!first.Contains("YAPI KREDİ PORTFÖY YÖNETİMİ A.Ş.")
            || !first.Contains($"({fundCode})") || !normalized.Contains(month)
            || !first.Contains("Rayiç Değeri") || !first.Contains("A) HİSSE SENETLERİ"))
            throw new InvalidDataException("Yapı Kredi PDF fon kimliği, dönemi veya tablo formatı doğrulanamadı.");
        var navMatch = Regex.Match(first, $@":\s*(?<nav>{number})\s+Ç\. TOPLAM DEĞER/NET");
        if (!navMatch.Success) throw new InvalidDataException("PDF net fon değeri okunamadı.");
        var nav = decimal.Parse(navMatch.Groups["nav"].Value, CultureInfo.InvariantCulture);
        if (nav <= 0) throw new InvalidDataException("PDF net fon değeri geçersiz.");
        var rowPattern = new Regex($@"\s(?<nominal>{number})\s+(?<value>{number})\s+\d+\.\d{{2}}\s*TR[A-Z0-9]{{10}}\s*(?<symbol>[A-Z0-9]{{3,5}})\s*$");
        var totalPattern = new Regex($@"^\s*{number}\s+(?<total>{number})\s+\d+\.\d{{2}}\s*TOPLAM\s*$");
        var values = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var started = false;
        foreach (var page in pages)
        {
            var text = page;
            if (!started)
            {
                var start = text.IndexOf("A) HİSSE SENETLERİ", StringComparison.Ordinal);
                if (start < 0) continue;
                text = text[(start + "A) HİSSE SENETLERİ".Length)..];
                started = true;
            }
            foreach (var line in text.Split('\n'))
            {
                var total = totalPattern.Match(line.Trim());
                if (total.Success)
                {
                    var reported = decimal.Parse(total.Groups["total"].Value, CultureInfo.InvariantCulture);
                    if (Math.Abs(values.Values.Sum() - reported) > 0.05m)
                        throw new InvalidDataException("Yapı Kredi hisse tutarları rapor toplamıyla uyuşmuyor.");
                    var result = values.Select(x => new ParsedFundHolding(x.Key, decimal.Round(x.Value / nav * 100m, 2)))
                        .Where(x => x.WeightPercent > 0).OrderByDescending(x => x.WeightPercent).ToList();
                    if (!KapPortfolioDiscovery.HasValidHoldings(result))
                        throw new InvalidDataException("Yapı Kredi net hisse ağırlıkları doğrulanamadı.");
                    return result;
                }
                if (line.Contains("TOPLAM") || Regex.IsMatch(line.Trim(), @"^:?\s*[A-ZÇŞÖÜİ]\)"))
                    throw new InvalidDataException("Yapı Kredi hisse tablosu toplamı bulunamadı.");
                var row = rowPattern.Match(line.TrimEnd());
                if (!row.Success)
                {
                    if (Regex.IsMatch(line, @"TR[A-Z0-9]{10}"))
                        throw new InvalidDataException("Yapı Kredi hisse satırı okunamadı; eksik tablo kabul edilmedi.");
                    continue;
                }
                var symbol = row.Groups["symbol"].Value;
                values[symbol] = values.GetValueOrDefault(symbol)
                    + decimal.Parse(row.Groups["value"].Value, CultureInfo.InvariantCulture);
            }
        }
        throw new InvalidDataException("Yapı Kredi hisse tablosu tamamlanamadı.");
    }
}
