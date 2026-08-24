using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.WordGames;

public interface IDailyWordGameService
{
    Task<DailyWordGameDto?> GetTodayAsync(
        string identityUserId,
        CancellationToken cancellationToken = default);

    Task<SubmitWordGuessResultDto> SubmitGuessAsync(
        string identityUserId,
        string guess,
        CancellationToken cancellationToken = default);
    Task EnsureTodayPuzzleAsync(
    CancellationToken cancellationToken = default);
}