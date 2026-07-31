namespace MiniMola.Application.Shop;

public interface IFishShopService
{
    Task<FishShopDto?> GetShopAsync(
        string identityUserId,
        CancellationToken cancellationToken = default);

    Task<PurchaseFishResultDto> PurchaseFishAsync(
        string identityUserId,
        int fishSpeciesId,
        CancellationToken cancellationToken = default);
}