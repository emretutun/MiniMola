using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniMola.Application.Markets;
using MiniMola.Infrastructure;
using MiniMola.Infrastructure.Persistence;
using MiniMola.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

internal static class LiveCheck
{
    // Explicit opt-in: calls production services, updating catalog, quotes and estimates.
    public static async Task RunAsync(bool portfoliosOnly = false, bool healthOnly = false)
    {
        var connection = Environment.GetEnvironmentVariable("MINIMOLA_CHECK_CONNECTION")
            ?? throw new InvalidOperationException("Set MINIMOLA_CHECK_CONNECTION for the target development DB.");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddInfrastructure(connection);
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        if (healthOnly)
        {
            const string noUser = "health-check-nonexistent-user";
            var health = scope.ServiceProvider.GetRequiredService<IMarketDataHealthService>();
            var rows = await health.GetAsync(noUser, timeout.Token);
            var featured = await db.MarketAssets.Where(a => a.IsFeatured && a.IsActive).Select(a => a.Id).ToListAsync(timeout.Token);
            if (rows.Any(r => !featured.Contains(r.AssetId))) throw new InvalidOperationException("Other user's favorite leaked");
            if (await health.RetryAsync(noUser, int.MaxValue, timeout.Token) is not null) throw new InvalidOperationException("Unknown asset accepted");
            if (rows.Count > 0)
            {
                scope.ServiceProvider.GetRequiredService<IMemoryCache>().Set($"market-health-retry:{rows[0].AssetId}", true, TimeSpan.FromMinutes(2));
                var message = await health.RetryAsync(noUser, rows[0].AssetId, timeout.Token);
                if (message?.Contains("2 dakika") != true) throw new InvalidOperationException("Cooldown not enforced");
            }
            Console.WriteLine($"PASS health read query: {rows.Count} public featured rows, favorite isolation, missing asset and cooldown; no upstream calls.");
            return;
        }
        if (portfoliosOnly)
        {
            foreach (var code in new[] { "THF", "AK3", "TI2", "HVS", "NNF", "YEF", "MAC" })
            {
                var fundId = await db.MarketAssets.Where(x => x.ProviderSymbol == code && x.DataProviderCode == "TEFAS_YAT")
                    .Select(x => x.Id).SingleAsync(timeout.Token);
                var data = await scope.ServiceProvider.GetRequiredService<IFundPortfolioService>().GetLatestAsync(fundId, timeout.Token);
                Console.WriteLine($"{code}: available={data?.IsAvailable}, holdings={data?.Holdings.Count}, matched={data?.MatchedWeightPercent}; {data?.Message}");
                if (code != "MAC" && data?.IsAvailable != true) throw new InvalidOperationException(code + " portfolio missing");
                if (code == "MAC" && data?.IsAvailable != false) throw new InvalidOperationException("Unknown format must fail closed");
            }
            return;
        }
        var id = await db.MarketAssets.Where(x => x.Symbol == "THF" && x.DataProviderCode == "TEFAS_YAT")
            .Select(x => x.Id).SingleAsync(timeout.Token);
        var portfolio = await scope.ServiceProvider.GetRequiredService<IFundPortfolioService>()
            .GetLatestAsync(id, timeout.Token);
        Console.WriteLine($"Holdings {portfolio!.Holdings.Count}, matched {portfolio.Holdings.Count(x => x.MarketAssetId.HasValue)}, weight {portfolio.MatchedWeightPercent}");
        Console.WriteLine(portfolio.Message);
        if (!portfolio.IsAvailable || portfolio.Message?.Contains("otomatik kontrolü") != true)
            throw new InvalidOperationException("Live KAP discovery did not succeed.");
        // Read-only failure injection against the existing report; no synthetic records.
        using var failureCache = new MemoryCache(new MemoryCacheOptions());
        using var failingClient = new HttpClient(new FailedKapHandler()) { BaseAddress = new Uri("https://www.kap.org.tr/") };
        var fallbackService = new KapFundPortfolioService(db, failingClient, new KapFundPortfolioPdfParser(),
            scope.ServiceProvider.GetRequiredService<KapBistMarketAssetCatalogProvider>(), failureCache,
            NullLogger<KapFundPortfolioService>.Instance);
        var fallback = await fallbackService.GetLatestAsync(id, timeout.Token);
        if (fallback?.IsAvailable != true || fallback.DocumentUrl != portfolio.DocumentUrl
            || fallback.Holdings.Count != portfolio.Holdings.Count
            || fallback.Message?.Contains("son geçerli rapor") != true)
            throw new InvalidOperationException("Last valid report fallback failed.");
        Console.WriteLine("PASS HTTP failure preserves last valid report and exposes warning");
        var result = await scope.ServiceProvider.GetRequiredService<IFundEstimateService>()
            .GetEstimateAsync(id, timeout.Token);
        Console.WriteLine($"Model {result!.ModelVersion}, available {result.IsAvailable}, coverage {result.CoveragePercent}");
        Console.WriteLine(result.Message);
        var history = await scope.ServiceProvider.GetRequiredService<IFundEstimateService>()
            .GetHistoryAsync(id, timeout.Token);
        Console.WriteLine($"History endpoint completed: {history is not null}");
        Console.WriteLine($"History rows: {history!.Items.Count}, measured closing count: {history.EvaluatedCount}");
        foreach (var item in history.Items.Take(3))
            Console.WriteLine($"{item.TargetDate}: {item.Kind}, {item.Status}");
    }

    private sealed class FailedKapHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable));
    }
}
