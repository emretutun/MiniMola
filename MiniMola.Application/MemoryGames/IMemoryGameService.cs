using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.MemoryGames;

public interface IMemoryGameService
{
    Task<MemoryGameStatusDto?> GetStatusAsync(
        string identityUserId,
        CancellationToken cancellationToken = default);

    Task<MemoryGameResultDto> CompleteAsync(
        string identityUserId,
        int matchedPairs,
        int moveCount,
        CancellationToken cancellationToken = default);
}