using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.Aquariums;

public sealed record UpdateFishNicknameRequest(
    string Nickname);

public sealed record UpdateFishNicknameResultDto(
    bool Success,
    string Message,
    int UserFishId,
    string Nickname);