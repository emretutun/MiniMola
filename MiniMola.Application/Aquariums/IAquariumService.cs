using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.Aquariums;

public interface IAquariumService
{
    Task<AquariumDetailsDto?> GetByIdentityUserIdAsync(
        string identityUserId,
        CancellationToken cancellationToken = default);

    Task<AquariumUpgradeDto?> GetUpgradeStatusAsync(
    string identityUserId,
    CancellationToken cancellationToken = default);

    Task<AquariumUpgradeDto> UpgradeAsync(
        string identityUserId,
        CancellationToken cancellationToken = default);

    Task<UpdateFishNicknameResultDto>
    UpdateFishNicknameAsync(
        string identityUserId,
        int userFishId,
        string nickname,
        CancellationToken cancellationToken = default);

    Task<UpdateDecorationPositionResultDto>
    UpdateDecorationPositionAsync(
        string identityUserId,
        int userDecorationId,
        float positionX,
        float positionY,
        CancellationToken cancellationToken = default);

    Task<FeedFishResultDto> FeedFishAsync(
    string identityUserId,
    int userFishId,
    CancellationToken cancellationToken = default);

}