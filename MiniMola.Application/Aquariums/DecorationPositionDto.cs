using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.Aquariums;

public sealed record UpdateDecorationPositionRequest(
    float PositionX,
    float PositionY);

public sealed record UpdateDecorationPositionResultDto(
    bool Success,
    string Message,
    int UserDecorationId,
    float PositionX,
    float PositionY);
