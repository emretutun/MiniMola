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

        services.AddMemoryCache();

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


        services.AddScoped<ISpotifyConnectionService, SpotifyConnectionService>();

        return services;
    }
}