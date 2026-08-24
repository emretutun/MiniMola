using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiniMola.Domain.Entities;

namespace MiniMola.Infrastructure.Persistence;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext(options)
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<Aquarium> Aquariums => Set<Aquarium>();

    public DbSet<FishSpecies> FishSpecies => Set<FishSpecies>();

    public DbSet<UserFish> UserFish => Set<UserFish>();

    public DbSet<DecorationItem> DecorationItems => Set<DecorationItem>();

    public DbSet<UserDecoration> UserDecorations => Set<UserDecoration>();

    public DbSet<PointTransaction> PointTransactions => Set<PointTransaction>();

    public DbSet<DailyWordPuzzle> DailyWordPuzzles
    => Set<DailyWordPuzzle>();

    public DbSet<WordPoolItem> WordPoolItems
    => Set<WordPoolItem>();

    public DbSet<WordGameSession> WordGameSessions
        => Set<WordGameSession>();

    public DbSet<WordGameGuess> WordGameGuesses
        => Set<WordGameGuess>();
    public DbSet<SpotifyConnection> SpotifyConnections
    => Set<SpotifyConnection>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}