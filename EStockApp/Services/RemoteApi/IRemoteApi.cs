using EStockApp.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EStockApp.Services.RemoteApi;

public interface IRemoteApi
{
    IAsyncEnumerable<OrderItemsRemoteResult> GetHistoriesAsync(DateOnly startDate);

    IAsyncEnumerable<OrderRemoteResult> GetOrdersAsync(DateOnly startDate);

    Task<OrderSyncModel?> GetOrderAsync(string id, bool loadItems = false);

    Task<RemoteResult<ProductDetailModel>> GetProductDetailAsync(string productCode);
}
