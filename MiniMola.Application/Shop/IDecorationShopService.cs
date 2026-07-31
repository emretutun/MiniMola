using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.Shop;

public interface IDecorationShopService
{
    Task<DecorationShopDto?> GetShopAsync(
        string identityUserId,
        CancellationToken cancellationToken = default);

    Task<PurchaseDecorationResultDto> PurchaseAsync(
        string identityUserId,
        int decorationItemId,
        CancellationToken cancellationToken = default);
}