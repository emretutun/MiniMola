using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MiniMola.Application.WordGames;
using MiniMola.Domain.Entities;
using MiniMola.Domain.Enums;
using MiniMola.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace MiniMola.Infrastructure.Services;

public sealed class DailyWordGameService(
    ApplicationDbContext dbContext,
    ILogger<DailyWordGameService> logger)
    : IDailyWordGameService
{
    private const string TurkishAlphabet =
        "ABCÇDEFGĞHIİJKLMNOÖPRSŞTUÜVYZ";

    private static readonly CultureInfo TurkishCulture =
        CultureInfo.GetCultureInfo("tr-TR");

    private static readonly TimeZoneInfo TurkeyTimeZone =
        FindTurkeyTimeZone();

    public async Task<DailyWordGameDto?> GetTodayAsync(
        string identityUserId,
        CancellationToken cancellationToken = default)
    {
        var today = GetTurkeyToday();

        var puzzle =
            await GetOrCreateDailyPuzzleAsync(
                today,
                cancellationToken);

        if (puzzle is null)
        {
            return null;
        }

        var profile = await dbContext.UserProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.IdentityUserId == identityUserId,
                cancellationToken);

        if (profile is null)
        {
            return null;
        }

        var session = await dbContext.WordGameSessions
            .SingleOrDefaultAsync(
                x => x.UserProfileId == profile.Id
                     && x.DailyWordPuzzleId == puzzle.Id,
                cancellationToken);

        if (session is null)
        {
            session = new WordGameSession
            {
                UserProfileId = profile.Id,
                DailyWordPuzzleId = puzzle.Id,
                Status = WordGameStatus.InProgress
            };

            dbContext.WordGameSessions.Add(session);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return await BuildGameDtoAsync(
            profile,
            puzzle,
            session,
            cancellationToken);
    }

    public async Task<SubmitWordGuessResultDto> SubmitGuessAsync(
        string identityUserId,
        string guess,
        CancellationToken cancellationToken = default)
    {
        var normalizedGuess = NormalizeWord(guess);

        if (!IsValidGuess(normalizedGuess))
        {
            return Failed(
                "Tahmin beş Türkçe harften oluşmalıdır.");
        }

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        var today = GetTurkeyToday();

        var puzzle =
            await GetOrCreateDailyPuzzleAsync(
                today,
                cancellationToken);

        if (puzzle is null)
        {
            return Failed(
                "Aktif kelime havuzunda kullanılabilecek kelime bulunamadı.");
        }

        var profile = await dbContext.UserProfiles
            .SingleOrDefaultAsync(
                x => x.IdentityUserId == identityUserId,
                cancellationToken);

        if (profile is null)
        {
            return Failed(
                "Kullanıcı profili bulunamadı.");
        }

        var session = await dbContext.WordGameSessions
            .SingleOrDefaultAsync(
                x => x.UserProfileId == profile.Id
                     && x.DailyWordPuzzleId == puzzle.Id,
                cancellationToken);

        if (session is null)
        {
            session = new WordGameSession
            {
                UserProfileId = profile.Id,
                DailyWordPuzzleId = puzzle.Id,
                Status = WordGameStatus.InProgress
            };

            dbContext.WordGameSessions.Add(session);
        }

        if (session.Status != WordGameStatus.InProgress)
        {
            var completedGame = await BuildGameDtoAsync(
                profile,
                puzzle,
                session,
                cancellationToken);

            return new SubmitWordGuessResultDto(
                false,
                "Bugünkü oyun zaten tamamlandı.",
                profile.PointBalance,
                completedGame);
        }

        if (session.Id > 0)
        {
            var wasAlreadyGuessed =
                await dbContext.WordGameGuesses.AnyAsync(
                    x => x.WordGameSessionId == session.Id
                         && x.Guess == normalizedGuess,
                    cancellationToken);

            if (wasAlreadyGuessed)
            {
                var currentGame = await BuildGameDtoAsync(
                    profile,
                    puzzle,
                    session,
                    cancellationToken);

                return new SubmitWordGuessResultDto(
                    false,
                    "Bu kelimeyi daha önce denedin.",
                    profile.PointBalance,
                    currentGame);
            }
        }

        var resultPattern = EvaluateGuess(
            puzzle.Word,
            normalizedGuess);

        session.AttemptCount++;

        var gameGuess = new WordGameGuess
        {
            WordGameSession = session,
            Guess = normalizedGuess,
            ResultPattern = resultPattern,
            AttemptNumber = session.AttemptCount
        };

        dbContext.WordGameGuesses.Add(gameGuess);

        var isWon = resultPattern.All(x => x == '2');
        var isLastAttempt =
            session.AttemptCount >= puzzle.MaxAttempts;

        string message;

        if (isWon)
        {
            session.Status = WordGameStatus.Won;
            session.CompletedAtUtc = DateTime.UtcNow;

            if (!session.RewardGranted)
            {
                profile.PointBalance += puzzle.RewardPoints;
                profile.UpdatedAtUtc = DateTime.UtcNow;

                session.RewardGranted = true;

                dbContext.PointTransactions.Add(
                    new PointTransaction
                    {
                        UserProfileId = profile.Id,
                        TransactionType =
                            PointTransactionType.GameReward,
                        Amount = puzzle.RewardPoints,
                        BalanceAfter = profile.PointBalance,
                        ReferenceId =
                            $"word-game-{puzzle.Id}",
                        Description =
                            "Günlük kelime oyunu ödülü"
                    });
            }

            message =
                $"Tebrikler! {puzzle.RewardPoints} Damla kazandın.";
        }
        else if (isLastAttempt)
        {
            session.Status = WordGameStatus.Lost;
            session.CompletedAtUtc = DateTime.UtcNow;

            message =
                $"Tahmin hakların bitti. Kelime: {puzzle.Word}";
        }
        else
        {
            var remaining =
                puzzle.MaxAttempts - session.AttemptCount;

            message =
                $"Tahmin kaydedildi. {remaining} hakkın kaldı.";
        }

        session.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var game = await BuildGameDtoAsync(
            profile,
            puzzle,
            session,
            cancellationToken);

        return new SubmitWordGuessResultDto(
            true,
            message,
            profile.PointBalance,
            game);
    }

    public async Task EnsureTodayPuzzleAsync(
    CancellationToken cancellationToken = default)
    {
        var today = GetTurkeyToday();

        var puzzle =
            await GetOrCreateDailyPuzzleAsync(
                today,
                cancellationToken);

        if (puzzle is null)
        {
            throw new InvalidOperationException(
                "Bugünün kelime bulmacası hazırlanamadı. "
                + "Aktif kelime havuzunu kontrol et.");
        }

        logger.LogInformation(
            "Günlük kelime bulmacası hazırlandı. "
            + "PuzzleId: {PuzzleId}, "
            + "PuzzleDate: {PuzzleDate}",
            puzzle.Id,
            puzzle.PuzzleDate);
    }

    private async Task<DailyWordPuzzle?>
    GetOrCreateDailyPuzzleAsync(
        DateOnly puzzleDate,
        CancellationToken cancellationToken)
    {
        var ownsTransaction =
            dbContext.Database.CurrentTransaction is null;

        await using var transaction =
            ownsTransaction
                ? await dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken)
                : null;

        var existingPuzzle =
            await dbContext.DailyWordPuzzles
                .SingleOrDefaultAsync(
                    x => x.PuzzleDate == puzzleDate,
                    cancellationToken);

        if (existingPuzzle is not null)
        {
            if (transaction is not null)
            {
                await transaction.CommitAsync(
                    cancellationToken);
            }

            return existingPuzzle.IsActive
                ? existingPuzzle
                : null;
        }

        var wordPoolItem =
            await dbContext.WordPoolItems
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(
                    x => dbContext.DailyWordPuzzles
                        .Where(
                            puzzle =>
                                puzzle.WordPoolItemId == x.Id)
                        .Select(
                            puzzle =>
                                (DateOnly?)puzzle.PuzzleDate)
                        .Max())
                .ThenBy(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

        if (wordPoolItem is null)
        {
            if (transaction is not null)
            {
                await transaction.CommitAsync(
                    cancellationToken);
            }

            return null;
        }

        var newPuzzle = new DailyWordPuzzle
        {
            PuzzleDate = puzzleDate,
            Word = wordPoolItem.Word,
            Hint = wordPoolItem.Hint,
            RewardPoints = wordPoolItem.RewardPoints,
            MaxAttempts = wordPoolItem.MaxAttempts,
            IsActive = true,
            WordPoolItemId = wordPoolItem.Id
        };

        dbContext.DailyWordPuzzles.Add(newPuzzle);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(
                cancellationToken);
        }

        return newPuzzle;
    }

    private async Task<DailyWordGameDto> BuildGameDtoAsync(
        UserProfile profile,
        DailyWordPuzzle puzzle,
        WordGameSession session,
        CancellationToken cancellationToken)
    {
        var guesses = await dbContext.WordGameGuesses
            .AsNoTracking()
            .Where(x => x.WordGameSessionId == session.Id)
            .OrderBy(x => x.AttemptNumber)
            .Select(x => new WordGameGuessDto(
                x.AttemptNumber,
                x.Guess,
                x.ResultPattern))
            .ToListAsync(cancellationToken);

        var revealedAnswer =
            session.Status == WordGameStatus.InProgress
                ? null
                : puzzle.Word;

        return new DailyWordGameDto(
            puzzle.PuzzleDate,
            puzzle.Hint,
            puzzle.Word.Length,
            puzzle.RewardPoints,
            puzzle.MaxAttempts,
            session.Status.ToString(),
            session.AttemptCount,
            profile.PointBalance,
            revealedAnswer,
            guesses);
    }

    private static string NormalizeWord(string? word)
    {
        return (word ?? string.Empty)
            .Trim()
            .Normalize(NormalizationForm.FormC)
            .ToUpper(TurkishCulture);
    }

    private static bool IsValidGuess(string guess)
    {
        return guess.Length == 5
               && guess.All(TurkishAlphabet.Contains);
    }

    private static string EvaluateGuess(
        string answer,
        string guess)
    {
        var normalizedAnswer = NormalizeWord(answer);
        var answerCharacters = normalizedAnswer.ToCharArray();
        var guessCharacters = guess.ToCharArray();

        var result = Enumerable
            .Repeat('0', answerCharacters.Length)
            .ToArray();

        var usedAnswerCharacters =
            new bool[answerCharacters.Length];

        for (var index = 0;
             index < answerCharacters.Length;
             index++)
        {
            if (guessCharacters[index]
                != answerCharacters[index])
            {
                continue;
            }

            result[index] = '2';
            usedAnswerCharacters[index] = true;
        }

        for (var guessIndex = 0;
             guessIndex < guessCharacters.Length;
             guessIndex++)
        {
            if (result[guessIndex] == '2')
            {
                continue;
            }

            for (var answerIndex = 0;
                 answerIndex < answerCharacters.Length;
                 answerIndex++)
            {
                if (usedAnswerCharacters[answerIndex])
                {
                    continue;
                }

                if (guessCharacters[guessIndex]
                    != answerCharacters[answerIndex])
                {
                    continue;
                }

                result[guessIndex] = '1';
                usedAnswerCharacters[answerIndex] = true;
                break;
            }
        }

        return new string(result);
    }

    private static DateOnly GetTurkeyToday()
    {
        var turkeyTime = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            TurkeyTimeZone);

        return DateOnly.FromDateTime(turkeyTime);
    }

    private static TimeZoneInfo FindTurkeyTimeZone()
    {
        string[] timeZoneIds =
        [
            "Europe/Istanbul",
            "Turkey Standard Time"
        ];

        foreach (var timeZoneId in timeZoneIds)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                // Diğer platform kimliği denenecek.
            }
            catch (InvalidTimeZoneException)
            {
                // Diğer platform kimliği denenecek.
            }
        }

        return TimeZoneInfo.Utc;
    }

    private static SubmitWordGuessResultDto Failed(
        string message)
    {
        return new SubmitWordGuessResultDto(
            false,
            message,
            0,
            null);
    }
}