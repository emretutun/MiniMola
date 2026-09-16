using System.Text.Json;
using MiniMola.Application.Markets;
using MiniMola.Domain.Entities;
using MiniMola.Domain.Enums;
using MiniMola.Infrastructure.Services;

if (args.Contains("--live"))
{
    await LiveCheck.RunAsync();
    return;
}

var passed = 0;
void Check(bool value, string name)
{
    if (!value) throw new InvalidOperationException(name);
    Console.WriteLine($"PASS {name}");
    passed++;
}
var now = new DateTime(2026, 9, 16, 10, 0, 0, DateTimeKind.Utc);
var fund = new MarketAsset { Id = 1, Symbol = "THF", QuoteCurrency = "TRY" };
var portfolio = new FundPortfolioDto(1, true, true, new(2026, 8, 31), now.AddDays(-14),
    16, 80, 80, null, null, null,
    [new("AAA", "AAA", 60, 2), new("BBB", "BBB", 20, 3)]);
MarketPriceSnapshot Quote(int id, decimal price, decimal? change, DateTime at) => new()
{
    MarketAssetId = id, Price = price, DailyChangePercent = change, ObservedAtUtc = at,
    PriceKind = id == 1 ? MarketPriceKind.Official : MarketPriceKind.Delayed,
    Source = id == 1 ? "TEFAS / BEFAS" : "Yahoo Finance"
};
var prices = new[] { Quote(1, 10, null, now.Date.AddHours(12)),
    Quote(2, 102, 2, now.AddMinutes(-15)), Quote(3, 99, -1, now.AddMinutes(-15)) };
FundEstimateDto Calculate(FundPortfolioDto? p = null, DateTime? clock = null) =>
    FundHoldingsEstimateCalculator.Calculate(fund, p ?? portfolio, prices, clock ?? now);
var result = Calculate();
Check(result.IsAvailable && result.EstimatedChangePercent == 1m && result.EstimatedPrice == 10.1m,
    "weighted positive and negative contributions; no scaling missing 20 percent");
Check(result.CoveragePercent == 80 && result.ModelVersion == "kap-holdings-v2", "coverage and model identity");
Check(result.Contributions.Last().WeightPercent == 20 && !result.Contributions.Last().IsCovered, "unmodelled remainder");
Check(!Calculate(portfolio with { ReportAgeDays = 46 }).IsAvailable, "old report rejected");
prices[1].ObservedAtUtc = now.AddHours(-2);
Check(!Calculate().IsAvailable && Calculate().CoveragePercent == 20, "stale quote excluded");
prices[1].ObservedAtUtc = now.AddMinutes(1);
Check(!Calculate().IsAvailable, "future quote excluded");
prices[1].ObservedAtUtc = now.AddMinutes(-15);
prices[0].ObservedAtUtc = now.AddDays(-1);
Check(!Calculate().IsAvailable, "old fund base rejected");
prices[0].ObservedAtUtc = now.Date;
prices[0].PriceKind = MarketPriceKind.Estimated;
Check(!Calculate().IsAvailable, "estimated base rejected");
prices[0].PriceKind = MarketPriceKind.Official;
Check(!Calculate(portfolio with { Holdings = [new("AAA", "AAA", 110, 2)] }).IsAvailable,
    "invalid weight rejected");
Check(!Calculate(clock: now.AddDays(3)).IsAvailable, "weekend rejected");
prices[1].ObservedAtUtc = now.Date.AddHours(15).AddMinutes(10);
prices[2].ObservedAtUtc = prices[1].ObservedAtUtc;
Check(Calculate(clock: now.Date.AddHours(19)).IsAvailable, "same day closing quotes after session accepted");

long Epoch(DateTime date) => new DateTimeOffset(date).ToUnixTimeSeconds();
JsonElement Chart(decimal? previous, decimal?[] closes) => JsonSerializer.SerializeToElement(new
{
    meta = new { previousClose = previous, chartPreviousClose = 50m, gmtoffset = 10800 },
    timestamp = new[] { Epoch(now.AddDays(-2)), Epoch(now.AddDays(-1)), Epoch(now) },
    indicators = new { quote = new[] { new { close = closes } } }
});
Check(YahooDailyChange.Calculate(Chart(null, [90, 100, 110]), 110, Epoch(now)) == 10,
    "uses prior session close, not range-start chartPreviousClose");
Check(YahooDailyChange.Calculate(Chart(100, [90, 90, 110]), 110, Epoch(now)) == 10,
    "explicit previous close takes precedence");
Check(YahooDailyChange.Calculate(Chart(null, [90, null, 110]), 110, Epoch(now)) is null,
    "missing prior close must not produce multi-day return");
var metaOnly = JsonSerializer.SerializeToElement(new { meta = new { regularMarketChangePercent = -3.019m } });
Check(YahooDailyChange.Calculate(metaOnly, 67.45m, Epoch(now)) == -3.019m,
    "provider daily change fallback when previous bar is missing");
var companies = KapBistMarketAssetCatalogProvider.ParseCompanies("""
    <a href="/tr/sirket-bilgileri/ozet/1"><div>ISATR</div><div> ISBTR</div><div> ISCTR</div></a></td><td><a href="/tr/sirket-bilgileri/ozet/1">Banka</a>
    <a href="/tr/sirket-bilgileri/ozet/2"><div>TERA, TRA</div></a></td><td><a href="/tr/sirket-bilgileri/ozet/2">Tera</a>
    """);
Check(companies.Select(x => x.Code).Order().SequenceEqual(new[] { "ISATR", "ISBTR", "ISCTR", "TERA", "TRA" }),
    "KAP multi-div and comma-separated codes retained");
Console.WriteLine($"{passed} checks passed.");
