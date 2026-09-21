using System.Text.Json;
using MiniMola.Application.Markets;
using MiniMola.Domain.Entities;
using MiniMola.Domain.Enums;
using MiniMola.Infrastructure.Services;

if (args.Length == 2 && args[0] == "--pdf-check")
{
    var expected = new Dictionary<string, (int Count, decimal Weight)>
    { ["THF"] = (77, 97.79m), ["AK3"] = (24, 92.26m), ["TI2"] = (51, 91.17m),
      ["HVS"] = (30, 94.89m), ["NNF"] = (27, 95.36m), ["YEF"] = (32, 92.00m) };
    foreach (var code in expected.Keys)
    {
        var holdings = new KapFundPortfolioPdfParser().ParseValidated(
            File.ReadAllBytes(Path.Combine(args[1], code + ".pdf")), code, new DateOnly(2026, 8, 31));
        Console.WriteLine($"{code}: {holdings.Count} holdings, weight {holdings.Sum(x => x.WeightPercent)}");
        if (holdings.Count != expected[code].Count || holdings.Sum(x => x.WeightPercent) != expected[code].Weight)
            throw new InvalidOperationException(code + " PDF regression");
    }
    var sample = File.ReadAllBytes(Path.Combine(args[1], "TI2.pdf"));
    foreach (var (code, date) in new[] { ("THF", new DateOnly(2026, 8, 31)), ("TI2", new DateOnly(2026, 7, 31)) })
    {
        var rejected = false;
        try { new KapFundPortfolioPdfParser().ParseValidated(sample, code, date); }
        catch (InvalidDataException) { rejected = true; }
        if (!rejected) throw new InvalidOperationException("Wrong fund/month PDF was accepted.");
    }
    Console.WriteLine("PASS PDF identity and period mismatch rejected");
    return;
}

if (args.Length == 2 && args[0] == "--inspect-pdf")
{
    using var pdf = UglyToad.PdfPig.PdfDocument.Open(args[1]);
    foreach (var page in pdf.GetPages().Take(12))
    {
        var text = UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor.ContentOrderTextExtractor.GetText(page);
        Console.WriteLine($"PAGE {page.Number}");
        if (page.Number <= 3) Console.WriteLine(text);
        foreach (var line in text.Split('\n').Where(x => x.Contains("GRUP TOPLAMI"))) Console.WriteLine(line);
    }
    return;
}

if (args.Contains("--live") || args.Contains("--live-portfolios") || args.Contains("--health-live"))
{
    await LiveCheck.RunAsync(args.Contains("--live-portfolios"), args.Contains("--health-live"));
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
var closeTime = new DateTime(2026, 9, 16, 15, 50, 0, DateTimeKind.Utc);
var closingResult = result with
{
    CalculatedAtUtc = closeTime,
    Contributions = result.Contributions.Select(x => x.IsCovered
        ? x with { ProxyObservedAtUtc = closeTime.Date.AddHours(15).AddMinutes(9) } : x).ToArray()
};
Check(FundEstimateTrackingPolicy.GetKind(result) == FundEstimateKind.Intraday, "daytime snapshot classified separately");
Check(FundEstimateTrackingPolicy.GetKind(closingResult) == FundEstimateKind.Closing, "closing snapshot with sufficient closing quotes");
Check(FundEstimateTrackingPolicy.GetKind(closingResult with { CalculatedAtUtc = closeTime.AddMinutes(-20) }) is null,
    "delay buffer before closing capture");
Check(FundEstimateTrackingPolicy.GetKind(closingResult with { CalculatedAtUtc = closeTime.AddHours(1) }) is null,
    "no late backfilled closing estimate");
Check(FundEstimateTrackingPolicy.GetKind(closingResult with { ModelVersion = "allocation-proxy-v1" }) is null,
    "proxy model not mislabelled as BIST closing estimate");
Check(FundEstimateTrackingPolicy.GetKind(closingResult with { Contributions = [closingResult.Contributions[1]] }) is null,
    "closing requires 60 percent coverage");
Check(FundEstimateTrackingPolicy.GetKind(closingResult with { Contributions = closingResult.Contributions.Select(x =>
    x with { ProxyObservedAtUtc = closeTime.AddHours(-2) }).ToArray() }) is null,
    "intraday prices never qualify as closing quotes");
Check(!FundEstimateTrackingPolicy.IsClosingWindow(closeTime.AddDays(3)), "weekend capture skipped");
Check(!FundEstimateTrackingPolicy.IsClosingQuote(closeTime.AddDays(-1), closeTime), "yesterday closing quote excluded");

FundEstimateSnapshot Estimate(FundEstimateKind kind) => new()
{
    MarketAssetId = 1, Kind = kind, TargetDate = new(2026, 9, 17), BasePrice = 10,
    BasePriceObservedAtUtc = now.Date, EstimatedChangePercent = 2m,
    EstimatedPrice = 10.2m, CalculatedAtUtc = closeTime, ModelVersion = "kap-holdings-v2"
};
var closed = Estimate(FundEstimateKind.Closing);
Check(!FundEstimateTrackingPolicy.CanReplace(closed, closingResult with { CalculatedAtUtc = closeTime.AddMinutes(10) }),
    "closing estimate immutable on subsequent requests");
Check(FundEstimateTrackingPolicy.CanReplace(Estimate(FundEstimateKind.Intraday), result with { CalculatedAtUtc = closeTime.AddMinutes(1) }),
    "newer intraday updates allowed");
Check(!FundEstimateTrackingPolicy.CanReplace(Estimate(FundEstimateKind.Intraday), result), "out of order request cannot overwrite newer record");
var actual = Quote(1, 10.1m, null, now.Date.AddDays(1).AddHours(12));
actual.CreatedAtUtc = now.Date.AddDays(1).AddHours(9);
var evaluationTime = now.Date.AddDays(1).AddHours(13);
Check(FundEstimateTrackingPolicy.TryEvaluate(closed, actual, evaluationTime)
    && closed.ActualChangePercent == 1m && closed.AbsoluteErrorPercent == 1m,
    "2 percent estimate versus 1 percent actual yields 1 percentage point error");
Check(!FundEstimateTrackingPolicy.TryEvaluate(closed, actual, evaluationTime), "evaluation idempotent");
Check(!FundEstimateTrackingPolicy.TryEvaluate(Estimate(FundEstimateKind.Legacy), actual, evaluationTime),
    "legacy records not promoted to new metrics");
actual.ObservedAtUtc = actual.ObservedAtUtc.AddDays(1);
Check(!FundEstimateTrackingPolicy.TryEvaluate(Estimate(FundEstimateKind.Closing), actual, evaluationTime.AddDays(1)),
    "wrong publication date cannot produce multi-day comparison");
actual.ObservedAtUtc = actual.ObservedAtUtc.AddDays(-1);
actual.PriceKind = MarketPriceKind.Estimated;
Check(!FundEstimateTrackingPolicy.TryEvaluate(Estimate(FundEstimateKind.Closing), actual, evaluationTime),
    "estimated price cannot count as actual");
actual.PriceKind = MarketPriceKind.Official;
actual.CreatedAtUtc = closeTime.AddMinutes(-1);
Check(!FundEstimateTrackingPolicy.TryEvaluate(Estimate(FundEstimateKind.Closing), actual, evaluationTime),
    "actual known before prediction rejected");
var searchJson = """
    [{"fundCode":"OTHER","subject":"Portföy Dağılım Raporu","disclosureIndex":99,"publishDate":"15.09.2026 11:00:00"},
     {"fundCode":"THF","subject":"Yatırımcı Bilgi Formu","disclosureIndex":98,"publishDate":"14.09.2026 11:00:00"},
     {"fundCode":"THF","subject":"Portföy Dağılım Raporu","disclosureIndex":1657113,"publishDate":"02.09.2026 11:02:16"},
     {"fundCode":"THF","subject":"Portföy Dağılım Raporu","disclosureIndex":100,"publishDate":"02.10.2026 11:00:00"}]
    """;
Check(KapPortfolioDiscovery.FindLatest(searchJson, now) == 1657113, "discovery filters fund, subject and future publication");
Check(KapPortfolioDiscovery.FindLatest("[]", now) is null, "empty discovery does not invent report");
var detailJson = """
    [{"disclosure":{"disclosureBasic":{"stockCode":"THF","title":"Portföy Dağılım Raporu",
      "disclosureIndex":1657113,"isBlocked":false,"year":2026,"donem":8,"publishDate":"2026.09.02 11:02:16"},
      "disclosureDetail":{"fundOid":"4028328c950ba8c70195140f682921da"}},
      "attachments":[{"objId":"4028328c9f52dc4001a06121e9864d61","fileName":"THF_2026.08.pdf","fileExtension":"pdf"}]}]
    """;
var discovered = KapPortfolioDiscovery.ReadDetail(detailJson, 1657113, now);
Check(discovered.ReportDate == new DateOnly(2026, 8, 31)
    && discovered.PublishedAtUtc == new DateTime(2026, 9, 2, 8, 2, 16, DateTimeKind.Utc), "report month end and Turkey UTC conversion");
bool RejectDetail(string json)
{
    try { KapPortfolioDiscovery.ReadDetail(json, 1657113, now); return false; }
    catch (Exception e) when (e is InvalidDataException or JsonException or InvalidOperationException or ArgumentOutOfRangeException) { return true; }
}
Check(RejectDetail(detailJson.Replace("\"THF\"", "\"OTHER\"")), "wrong fund rejected");
Check(RejectDetail(detailJson.Replace("\"isBlocked\":false", "\"isBlocked\":true")), "blocked report rejected");
Check(RejectDetail(detailJson.Replace("THF_2026.08.pdf", "THF_2026.07.pdf")), "wrong attachment month rejected");
Check(RejectDetail(detailJson.Replace("4028328c9f52dc4001a06121e9864d61", "../other")), "unsafe attachment ID rejected");
Check(RejectDetail(detailJson.Replace("\"donem\":8", "\"donem\":9")), "uncompleted report month rejected");
var validHoldings = Enumerable.Range(1, 5).Select(x => new ParsedFundHolding($"A{x}", 19)).ToArray();
Check(KapPortfolioDiscovery.HasValidHoldings(validHoldings), "complete conservative THF weight validation");
Check(!KapPortfolioDiscovery.HasValidHoldings([new("AAA", 2)]), "partial PDF parsing cannot replace valid report");
Check(!KapPortfolioDiscovery.HasValidHoldings(validHoldings.Select(x => x with { WeightPercent = 21 }).ToArray()), "excess weight rejected");
Check(!KapPortfolioDiscovery.HasValidHoldings(validHoldings.Select(x => x with { Symbol = "AAA" }).ToArray()), "duplicate PDF holdings rejected");
Check(KapPortfolioDiscovery.FindLatest(searchJson.Replace("THF", "TI2"), now, "TI2") == 1657113,
    "discovery is parameterized for other funds");
var ti2Oid = "33E5FED7E2D700EAE0530A4A622B2AEA";
var ti2Detail = detailJson.Replace("THF", "TI2").Replace(KapPortfolioDiscovery.FundOid, ti2Oid);
Check(KapPortfolioDiscovery.ReadDetail(ti2Detail, 1657113, now, "TI2", ti2Oid).FileName == "TI2_2026.08.pdf",
    "attachment identity uses selected fund and OID");
Check(RejectDetail(ti2Detail), "another fund attachment cannot become THF report");
var catalogJson = """
    {"fundCode":"TI2","fundOid":"33E5FED7E2D700EAE0530A4A622B2AEA","fundName":"İŞ PORTFÖY HİSSE SENEDİ (TL) FONU","fundClass":"HS","fundType":"YF","fundState":"Y"}
    {"fundCode":"YAB","fundOid":"33E5FED7E2D700EAE0530A4A622B2AEB","fundName":"YABANCI HİSSE FONU","fundClass":"HS","fundType":"YF","fundState":"Y"}
    {"fundCode":"ARB","fundOid":"33E5FED7E2D700EAE0530A4A622B2AEC","fundName":"ARBİTRAJ HİSSE FONU","fundClass":"HS","fundType":"YF","fundState":"Y"}
    {"fundCode":"SRB","fundOid":"33E5FED7E2D700EAE0530A4A622B2AED","fundName":"HİSSE SERBEST FONU","fundClass":"HS","fundType":"YF","fundState":"Y"}
    {"fundCode":"OLD","fundOid":"33E5FED7E2D700EAE0530A4A622B2AEE","fundName":"HİSSE FONU","fundClass":"HS","fundType":"YF","fundState":"N"}
    """;
var catalog = KapPortfolioDiscovery.ReadEquityCatalog(catalogJson.Replace("\"", "\\\""));
Check(catalog.Count == 1 && catalog["TI2"].Oid == ti2Oid, "escaped KAP catalog filters foreign, arbitrage, free and inactive funds");
bool RejectCatalog(string html)
{
    try { KapPortfolioDiscovery.ReadEquityCatalog(html); return false; }
    catch (InvalidDataException) { return true; }
}
Check(RejectCatalog("<html>service unavailable</html>"), "empty or changed catalog fails closed");
Check(RejectCatalog(catalogJson + catalogJson.Replace(ti2Oid, "33E5FED7E2D700EAE0530A4A622B2AFF")), "conflicting fund identities rejected");
var yefDetail = detailJson.Replace("THF_2026.08.pdf", "YEF.pdf").Replace("THF", "YEF");
Check(KapPortfolioDiscovery.ReadDetail(yefDetail, 1657113, now, "YEF").FileName == "YEF.pdf",
    "bare fund filename accepted with verified notification identity");
var yefText = "YAPI KREDİ PORTFÖY YÖNETİMİ A.Ş.\n(YEF) FON\nAĞUSTOS 2026\n: 1,000.00 Ç. TOPLAM DEĞER/NET\nRayiç Değeri\nA) HİSSE SENETLERİ\n"
    + string.Join('\n', new[] { "AAA", "BBB", "CCC", "DDD", "EEE" }.Select(s => $"Company 1.00 180.00 15.00TRAAEFES91A9{s}"))
    + "\n5.00 900.00 75.00TOPLAM\nO) DİĞER\nignored 999999.00";
var yefHoldings = KapFundPortfolioPdfParser.ParseYapiKrediText([yefText], "YEF", new(2026, 8, 31));
Check(yefHoldings.Count == 5 && yefHoldings.Sum(x => x.WeightPercent) == 90,
    "Yapi Kredi uses market value over NAV, not displayed portfolio percentages");
bool RejectYef(string text, string code = "YEF")
{
    try { KapFundPortfolioPdfParser.ParseYapiKrediText([text], code, new(2026, 8, 31)); return false; }
    catch (InvalidDataException) { return true; }
}
Check(RejectYef(yefText, "THF"), "Yapi Kredi wrong fund identity rejected");
Check(RejectYef(yefText.Replace("AĞUSTOS", "TEMMUZ")), "Yapi Kredi wrong month rejected");
Check(RejectYef(yefText.Replace("900.00", "901.00")), "Yapi Kredi missing market value rejected");
Check(RejectYef(yefText.Replace("1,000.00", "0.00")), "Yapi Kredi zero NAV rejected");
Check(RejectYef(yefText.Replace("180.00 15.00TRAAEFES91A9AAA", "-180.00 15.00TRAAEFES91A9AAA")), "negative unmodelled row rejected");
Check(MarketDataHealthService.GetPriceStatus(null, null, true, now) == "Fiyat yok", "health missing price");
Check(MarketDataHealthService.GetPriceStatus(now.AddMinutes(-21), 1, true, now).StartsWith("Eski"), "health crypto stale threshold");
Check(MarketDataHealthService.GetPriceStatus(now.AddDays(-3), 1, false, now) == "Fiyat kaydı var", "health noncrypto grace period");
Check(MarketDataHealthService.GetPriceStatus(now, null, false, now).Contains("günlük değişim yok"), "health missing daily change distinct");
Check(MarketDataHealthService.GetPriceStatus(now.AddMinutes(1), 1, false, now).Contains("gelecekte"), "health future timestamp rejected");
Console.WriteLine($"{passed} checks passed.");
