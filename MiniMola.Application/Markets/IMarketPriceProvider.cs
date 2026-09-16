using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.Markets;

public interface IMarketPriceProvider
{
    Task RefreshStalePricesAsync(
        IReadOnlyCollection<int> marketAssetIds,
        CancellationToken cancellationToken = default);
}