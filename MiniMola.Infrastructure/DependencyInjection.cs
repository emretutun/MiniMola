using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniMola.Application.Abstractions;
using MiniMola.Infrastructure.Persistence;
using MiniMola.Infrastructure.Services;
using MiniMola.Application.Aquariums;
using MiniMola.Application.Shop;
using MiniMola.Application.WordGames;
using MiniMola.Application.News;
using MiniMola.Application.Spotify;
using MiniMola.Application.BubbleGames;
using MiniMola.Application.MemoryGames;
using MiniMola.Application.WorkSchedules;
using MiniMola.Application.Emails;
using MiniMola.Infrastructure.Email;
using MiniMola.Application.Markets;

namespace MiniMola.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IAquariumService, AquariumService>();
        services.AddScoped<IFishShopService, FishShopService>();
        services.AddScoped<IDecorationShopService, DecorationShopService>();

        services.AddScoped<IDailyWordGameService, DailyWordGameService>();
        services.AddScoped<IBubbleGameService, BubbleGameService>();
        services.AddScoped<IMemoryGameService, MemoryGameService>();

        services.AddScoped<IWorkScheduleService,WorkScheduleService>();

        services.AddScoped<
        IMarketWatchlistService,
        MarketWatchlistService>();

        services.AddScoped<
            IMarketHistoryService,
            MarketHistoryService>();

        services.AddScoped<
            IMarketTechnicalAnalysisService,
            MarketTechnicalAnalysisService>();

        services.AddScoped<
            IFundEstimateService,
            FundEstimateService>();

        services.AddHttpClient(
            MarketHistoryService.YahooClientName,
            client =>
            {
                client.BaseAddress =
                    new Uri(
                        "https://query1.finance.yahoo.com/");

                client.Timeout =
                    TimeSpan.FromSeconds(12);

                client.DefaultRequestHeaders.Accept.ParseAdd(
                    "application/json");

                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "MiniMola/1.0");
            });

        services.AddHttpClient(
            MarketHistoryService.CoinGeckoClientName,
            client =>
            {
                client.BaseAddress =
                    new Uri(
                        "https://api.coingecko.com/api/v3/");

                client.Timeout =
                    TimeSpan.FromSeconds(20);

                client.DefaultRequestHeaders.Accept.ParseAdd(
                    "application/json");

                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "MiniMola/1.0");
            });

        services.AddHttpClient(
            MarketHistoryService.TefasClientName,
            client =>
            {
                client.BaseAddress =
                    new Uri("https://www.tefas.gov.tr/");

                client.Timeout =
                    TimeSpan.FromSeconds(30);

                client.DefaultRequestHeaders.Accept.ParseAdd(
                    "application/json");

                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "MiniMola/1.0");

                client.DefaultRequestHeaders.Referrer =
                    new Uri(
                        "https://www.tefas.gov.tr/tr/fon-verileri");

                client.DefaultRequestHeaders.TryAddWithoutValidation(
                    "Origin",
                    "https://www.tefas.gov.tr");
            });

        services.AddScoped<
            IMarketPriceRefreshService,
            MarketPriceRefreshService>();

        services.AddHttpClient<
            CoinGeckoMarketAssetCatalogSyncService>(
                client =>
                {
                    client.BaseAddress =
                        new Uri(
                            "https://api.coingecko.com/api/v3/");

                    client.Timeout =
                        TimeSpan.FromSeconds(20);

                    client.DefaultRequestHeaders.Accept.ParseAdd(
                        "application/json");

                    client.DefaultRequestHeaders.UserAgent.ParseAdd(
                        "MiniMola/1.0");
                });

        services.AddScoped<IMarketAssetCatalogProvider>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    CoinGeckoMarketAssetCatalogSyncService>());

        services.AddHttpClient<
            KapBistMarketAssetCatalogProvider>(
                client =>
                {
                    client.BaseAddress =
                        new Uri("https://www.kap.org.tr/");

                    client.Timeout =
                        TimeSpan.FromSeconds(30);

                    client.DefaultRequestHeaders.Accept.ParseAdd(
                        "text/html");

                    client.DefaultRequestHeaders.UserAgent.ParseAdd(
                        "MiniMola/1.0");
                });

        services.AddScoped<IMarketAssetCatalogProvider>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    KapBistMarketAssetCatalogProvider>());

        services.AddHttpClient<
            KapFundMarketAssetCatalogProvider>(
                client =>
                {
                    client.BaseAddress =
                        new Uri("https://www.kap.org.tr/");

                    client.Timeout =
                        TimeSpan.FromSeconds(45);

                    client.DefaultRequestHeaders.Accept.ParseAdd(
                        "text/html");

                    client.DefaultRequestHeaders.UserAgent.ParseAdd(
                        "MiniMola/1.0");
                });

        services.AddScoped<IMarketAssetCatalogProvider>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    KapFundMarketAssetCatalogProvider>());

        services.AddSingleton<KapFundPortfolioPdfParser>();
        services.AddScoped<IMarketDataHealthService, MarketDataHealthService>();

        services.AddHttpClient<
            IFundPortfolioService,
            KapFundPortfolioService>(
                client =>
                {
                    client.BaseAddress =
                        new Uri("https://www.kap.org.tr/");

                    client.Timeout =
                        TimeSpan.FromSeconds(45);

                    client.DefaultRequestHeaders.Accept.ParseAdd(
                        "application/json, application/pdf");

                    client.DefaultRequestHeaders.UserAgent.ParseAdd(
                        "MiniMola/1.0");

                    client.DefaultRequestHeaders.Referrer =
                        new Uri("https://www.kap.org.tr/");
                });

        services.AddScoped<
            IMarketAssetCatalogSyncService,
            MarketAssetCatalogSyncService>();

        services.AddHttpClient<
            CoinGeckoMarketPriceRefreshService>(
                client =>
                {
                    client.BaseAddress =
                        new Uri(
                            "https://api.coingecko.com/api/v3/");

                    client.Timeout =
                        TimeSpan.FromSeconds(8);

                    client.DefaultRequestHeaders
                        .Accept
                        .ParseAdd("application/json");

                    client.DefaultRequestHeaders
                        .UserAgent
                        .ParseAdd("MiniMola/1.0");
                });

        services.AddScoped<IMarketPriceProvider>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    CoinGeckoMarketPriceRefreshService>());
        services.AddHttpClient<
    TcmbMarketPriceProvider>(
        client =>
        {
            client.BaseAddress =
                new Uri(
                    "https://www.tcmb.gov.tr/kurlar/");

            client.Timeout =
                TimeSpan.FromSeconds(8);

            client.DefaultRequestHeaders
                .Accept
                .ParseAdd("application/xml");

            client.DefaultRequestHeaders
                .UserAgent
                .ParseAdd("MiniMola/1.0");
        });

        services.AddScoped<IMarketPriceProvider>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    TcmbMarketPriceProvider>());




        services.AddMemoryCache();
        services.AddScoped<IEmailService,SmtpEmailService>();


        services.AddHttpClient<INewsService, HaberturkNewsService>(
            client =>
            {
                client.BaseAddress =
                    new Uri("https://www.haberturk.com/");

                client.Timeout = TimeSpan.FromSeconds(12);

                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "MiniMola/1.0");

                client.DefaultRequestHeaders.Accept.ParseAdd(
                    "application/rss+xml");

                client.DefaultRequestHeaders.Accept.ParseAdd(
                    "application/xml");
            });
        services.AddHttpClient(
        "SpotifyApi",
        client =>
        {
            client.BaseAddress =
                new Uri("https://api.spotify.com/v1/");

            client.Timeout = TimeSpan.FromSeconds(12);

            client.DefaultRequestHeaders.Accept.ParseAdd(
                "application/json");
        });

        services.AddHttpClient<
    YahooFinanceMarketPriceProvider>(
        client =>
        {
            client.BaseAddress =
                new Uri(
                    "https://query1.finance.yahoo.com/");

            client.Timeout =
                TimeSpan.FromSeconds(8);

            client.DefaultRequestHeaders
                .Accept
                .ParseAdd("application/json");

            client.DefaultRequestHeaders
                .UserAgent
                .ParseAdd("MiniMola/1.0");
        });

        services.AddScoped<IMarketPriceProvider>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    YahooFinanceMarketPriceProvider>());

        services.AddHttpClient<
            TefasMarketPriceProvider>(
                client =>
                {
                    client.BaseAddress =
                        new Uri("https://www.tefas.gov.tr/");

                    client.Timeout =
                        TimeSpan.FromSeconds(60);

                    client.DefaultRequestHeaders.Accept.ParseAdd(
                        "application/json");

                    client.DefaultRequestHeaders.UserAgent.ParseAdd(
                        "MiniMola/1.0");

                    client.DefaultRequestHeaders.Referrer =
                        new Uri(
                            "https://www.tefas.gov.tr/tr/fon-verileri");

                    client.DefaultRequestHeaders.TryAddWithoutValidation(
                        "Origin",
                        "https://www.tefas.gov.tr");
                });

        services.AddScoped<IMarketPriceProvider>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    TefasMarketPriceProvider>());


        services.AddScoped<ISpotifyConnectionService, SpotifyConnectionService>();

        return services;
    }
}
