using System.Globalization;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace MiniMola.Infrastructure.Services;

public sealed partial class KapFundPortfolioPdfParser
{
    public const string Version = "kap-equity-ftd-v3";

    public IReadOnlyList<ParsedFundHolding> ParseValidated(byte[] pdfBytes, string fundCode, DateOnly reportDate)
    {
        using var document = PdfDocument.Open(pdfBytes);
        var first = ContentOrderTextExtractor.GetText(document.GetPage(1));
        if (first.Contains("YAPI KREDİ PORTFÖY YÖNETİMİ A.Ş.") && first.Contains("Rayiç Değeri"))
            return ParseYapiKrediText(document.GetPages().Take(30).Select(p => ContentOrderTextExtractor.GetText(p)).ToArray(), fundCode, reportDate);
        var month = reportDate.ToString("MMMM-yyyy", CultureInfo.GetCultureInfo("tr-TR"));
        if (!Regex.IsMatch(first, $@"(?m)^\s*{Regex.Escape(fundCode)}\s*-", RegexOptions.IgnoreCase)
            || !first.Contains(month, StringComparison.OrdinalIgnoreCase)
            || !first.Contains("(FTD") || !first.Contains("III-FON PORTFÖY DEĞERİ TABLOSU"))
            throw new InvalidDataException("PDF kimliği, dönemi veya FTD tablo formatı doğrulanamadı.");

        var weights = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var started = false;
        // Transaction appendices can contain hundreds of pages; only scan the opening portfolio table.
        foreach (var page in document.GetPages().Take(30))
        {
            var text = ContentOrderTextExtractor.GetText(page);
            if (!started)
            {
                var start = text.IndexOf("Hisse Türk", StringComparison.Ordinal);
                if (start < 0) continue;
                text = text[(start + "Hisse Türk".Length)..];
                started = true;
            }
            var lines = text.Split('\n');
            var end = Array.FindIndex(lines, x => x.Contains("GRUP TOPLAMI"));
            var symbols = page.GetWords().Where(word => Math.Abs(word.BoundingBox.Left - 20d) <= 0.5d
                && SymbolPattern.IsMatch(word.Text)).Select(word => word.Text).ToHashSet(StringComparer.OrdinalIgnoreCase);
            ParsePage(string.Join('\n', end < 0 ? lines : lines.Take(end)), symbols, weights);
            if (end < 0) continue;
            var totalMatch = FinalWeightPattern.Match(lines[end].Trim());
            if (!totalMatch.Success) throw new InvalidDataException("Yerli hisse grup toplamı okunamadı.");
            var expected = decimal.Parse(totalMatch.Groups["weight"].Value, CultureInfo.GetCultureInfo("tr-TR"));
            // Each source row is rounded to two decimals; do not accept large missing sections.
            if (Math.Abs(weights.Values.Sum() - expected) > 0.5m)
                throw new InvalidDataException("Okunan hisseler KAP grup toplamıyla uyuşmuyor.");
            var result = weights.Where(x => x.Value != 0)
                .Select(x => new ParsedFundHolding(x.Key, decimal.Round(x.Value, 2)))
                .OrderByDescending(x => x.WeightPercent).ToList();
            if (!KapPortfolioDiscovery.HasValidHoldings(result)) throw new InvalidDataException("Yerli hisse ağırlıkları doğrulanamadı.");
            return result;
        }
        throw new InvalidDataException("Desteklenen yerli hisse tablosu tamamlanamadı.");
    }

    private static readonly Regex SymbolPattern =
        new(
            "^[A-Z0-9]{3,5}$",
            RegexOptions.Compiled);

    private static readonly Regex TradingDatePattern =
        new(
            "\\b\\d{2}/\\d{2}/\\d{2}\\b",
            RegexOptions.Compiled);

    private static readonly Regex FinalWeightPattern =
        new(
            "(?<![\\d.])(?<weight>-?\\d{1,3},\\d{2})" +
            "(?:[A-Z][A-Z0-9]{8,})?\\s*$",
            RegexOptions.Compiled);

    public IReadOnlyList<ParsedFundHolding> Parse(
        byte[] pdfBytes)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);

        if (pdfBytes.Length == 0)
        {
            throw new InvalidDataException(
                "KAP portföy PDF dosyası boş.");
        }

        var weights =
            new Dictionary<string, decimal>(
                StringComparer.OrdinalIgnoreCase);

        using var document = PdfDocument.Open(pdfBytes);

        foreach (var page in document.GetPages())
        {
            var pageSymbols =
                page.GetWords()
                    .Where(word =>
                        Math.Abs(
                            word.BoundingBox.Left - 20d)
                            <= 0.5d
                        && SymbolPattern.IsMatch(word.Text))
                    .Select(word => word.Text)
                    .ToHashSet(
                        StringComparer.OrdinalIgnoreCase);

            var text =
                ContentOrderTextExtractor.GetText(page);

            ParsePage(
                text,
                pageSymbols,
                weights);
        }

        return weights
            .Where(item => item.Value != 0)
            .Select(item =>
                new ParsedFundHolding(
                    item.Key,
                    decimal.Round(
                        item.Value,
                        2,
                        MidpointRounding.AwayFromZero)))
            .OrderByDescending(item => item.WeightPercent)
            .ThenBy(item => item.Symbol)
            .ToList();
    }

    private static void ParsePage(
        string text,
        IReadOnlySet<string> pageSymbols,
        IDictionary<string, decimal> weights)
    {
        string? currentSymbol = null;
        var remainingLinesForPosition = 0;

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();

            if (line.Length == 0)
            {
                continue;
            }

            var firstToken =
                line.Split(
                    ' ',
                    2,
                    StringSplitOptions.RemoveEmptyEntries)[0];

            if (pageSymbols.Contains(firstToken))
            {
                currentSymbol = firstToken;
                remainingLinesForPosition = 18;
            }

            if (currentSymbol is not null
                && TradingDatePattern.IsMatch(line))
            {
                var weightMatch =
                    FinalWeightPattern.Match(line);

                if (weightMatch.Success
                    && decimal.TryParse(
                        weightMatch.Groups["weight"].Value,
                        NumberStyles.Number,
                        CultureInfo.GetCultureInfo("tr-TR"),
                        out var weightPercent))
                {
                    weights.TryGetValue(
                        currentSymbol,
                        out var existingWeight);

                    weights[currentSymbol] =
                        existingWeight + weightPercent;

                    currentSymbol = null;
                    remainingLinesForPosition = 0;
                    continue;
                }
            }

            if (currentSymbol is not null)
            {
                remainingLinesForPosition--;

                if (remainingLinesForPosition <= 0)
                {
                    currentSymbol = null;
                }
            }
        }
    }
}

public sealed record ParsedFundHolding(
    string Symbol,
    decimal WeightPercent);
