using EStockApp.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EStockApp.Services.OrderSync;

public interface IOrderHistorySync
{
    IAsyncEnumerable<OrderProductHistorySyncResult> GetHistoriesAsync(DateOnly startDate);

    IAsyncEnumerable<OrderInfoSyncResult> GetOrderListAsync(DateOnly startDate);

    Task<OrderInfoSyncModel> GetOrderAsync(string id, bool loadItems = false);
}
