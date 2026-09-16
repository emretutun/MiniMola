using System.Globalization;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace MiniMola.Infrastructure.Services;

public sealed class KapFundPortfolioPdfParser
{
    public const string Version = "tera-pdf-v1";

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
