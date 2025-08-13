using EStockApp.Models;

namespace EStockApp.Services.OrderSync;

public class OrderProductHistorySyncResult : SyncResult<OrderProductItemSyncModel>
{
    public OrderProductHistorySyncResult(OrderProductItemSyncModel result, string? message = null) : base(result, message)
    {
    }

    public OrderProductHistorySyncResult(SyncStatus status, string? message = null) : base(status, message)
    {
    }
}
