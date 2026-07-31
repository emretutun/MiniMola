using System;
using System.Collections.Generic;
using System.Text;
using MiniMola.Domain.Entities;

namespace MiniMola.Infrastructure.Persistence.Seed;

internal static class DailyWordPuzzleSeed
{
    private static readonly DateTime SeedDate =
        new(2026, 7, 29, 0, 0, 0, DateTimeKind.Utc);

    public static IReadOnlyCollection<DailyWordPuzzle> GetItems()
    {
        return
        [
            Create(
                1,
                new DateOnly(2026, 7, 29),
                "DENİZ",
                "Dalgalar ve kıyılarla birlikte düşün.",
                30),

            Create(
                2,
                new DateOnly(2026, 7, 30),
                "BALIK",
                "MiniMola akvaryumunun sakinlerinden biri.",
                30),

            Create(
                3,
                new DateOnly(2026, 7, 31),
                "NEFES",
                "Kısa bir molada yavaşlatmak iyi gelir.",
                30),

            Create(
                4,
                new DateOnly(2026, 8, 1),
                "HUZUR",
                "Sakinliğin bıraktığı güzel his.",
                30),

            Create(
                5,
                new DateOnly(2026, 8, 2),
                "KAHVE",
                "İş molalarının klasik eşlikçisi.",
                30),

            Create(
                6,
                new DateOnly(2026, 8, 3),
                "BULUT",
                "Gökyüzünde yavaşça süzülür.",
                30),

            Create(
                7,
                new DateOnly(2026, 8, 4),
                "GÜNEŞ",
                "Gündüz gökyüzünün en parlak misafiri.",
                30)
        ];
    }

    private static DailyWordPuzzle Create(
        int id,
        DateOnly puzzleDate,
        string word,
        string hint,
        int rewardPoints)
    {
        return new DailyWordPuzzle
        {
            Id = id,
            PuzzleDate = puzzleDate,
            Word = word,
            Hint = hint,
            RewardPoints = rewardPoints,
            MaxAttempts = 6,
            IsActive = true,
            CreatedAtUtc = SeedDate
        };
    }
}