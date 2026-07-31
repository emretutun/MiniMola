using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.MemoryGames;

public sealed record CompleteMemoryGameRequest(
    int MatchedPairs,
    int MoveCount);