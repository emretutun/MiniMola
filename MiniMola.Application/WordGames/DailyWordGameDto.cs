using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.WordGames;

public sealed record DailyWordGameDto(
    DateOnly PuzzleDate,
    string Hint,
    int WordLength,
    int RewardPoints,
    int MaxAttempts,
    string Status,
    int AttemptCount,
    int PointBalance,
    string? RevealedAnswer,
    IReadOnlyList<WordGameGuessDto> Guesses);

public sealed record WordGameGuessDto(
    int AttemptNumber,
    string Guess,
    string ResultPattern);

public sealed record SubmitWordGuessRequestDto(
    string Guess);

public sealed record SubmitWordGuessResultDto(
    bool Success,
    string Message,
    int PointBalance,
    DailyWordGameDto? Game);