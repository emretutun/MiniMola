using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.BubbleGames;

public interface IBubbleGameService
{
    Task<BubbleGameStatusDto?> GetStatusAsync(
        string identityUserId,
        CancellationToken cancellationToken = default);

    Task<BubbleGameResultDto> CompleteAsync(
        string identityUserId,
        int score,
        CancellationToken cancellationToken = default);
}
