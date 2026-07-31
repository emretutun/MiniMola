using System;
using System.Collections.Generic;
using System.Text;
using MiniMola.Domain.Common;

namespace MiniMola.Domain.Entities;

public sealed class WordGameGuess : BaseEntity
{
    public int WordGameSessionId { get; set; }

    public string Guess { get; set; } = string.Empty;

    public string ResultPattern { get; set; } = string.Empty;

    public int AttemptNumber { get; set; }

    public WordGameSession WordGameSession { get; set; } = null!;
}