using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniMola.Application.Markets;
using MiniMola.Infrastructure;
using MiniMola.Infrastructure.Persistence;

internal static class LiveCheck
{
    // Explicit opt-in: calls production services, updating catalog, quotes and estimates.
    public static async Task RunAsync()
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
        var id = await db.MarketAssets.Where(x => x.Symbol == "THF" && x.DataProviderCode == "TEFAS_YAT")
            .Select(x => x.Id).SingleAsync(timeout.Token);
        var portfolio = await scope.ServiceProvider.GetRequiredService<IFundPortfolioService>()
            .GetLatestAsync(id, timeout.Token);
        Console.WriteLine($"Holdings {portfolio!.Holdings.Count}, matched {portfolio.Holdings.Count(x => x.MarketAssetId.HasValue)}, weight {portfolio.MatchedWeightPercent}");
        var result = await scope.ServiceProvider.GetRequiredService<IFundEstimateService>()
            .GetEstimateAsync(id, timeout.Token);
        Console.WriteLine($"Model {result!.ModelVersion}, available {result.IsAvailable}, coverage {result.CoveragePercent}");
        Console.WriteLine(result.Message);
        var history = await scope.ServiceProvider.GetRequiredService<IFundEstimateService>()
            .GetHistoryAsync(id, timeout.Token);
        Console.WriteLine($"History endpoint completed: {history is not null}");
    }
}
