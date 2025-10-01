using EStockApp.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EStockApp.Services.OrderSync;

public interface IOrderHistorySync
{
    IAsyncEnumerable<OrderItemsSyncResult> GetHistoriesAsync(DateOnly startDate);

    IAsyncEnumerable<OrderSyncResult> GetOrdersAsync(DateOnly startDate);

    Task<OrderSyncModel> GetOrderAsync(string id, bool loadItems = false);
}
