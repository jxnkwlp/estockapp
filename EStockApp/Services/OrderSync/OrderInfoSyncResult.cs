using EStockApp.Models;

namespace EStockApp.Services.OrderSync;

public class OrderInfoSyncResult : SyncResult<OrderInfoSyncModel>
{
    public OrderInfoSyncResult(OrderInfoSyncModel result, string? message = null) : base(result, message)
    {
    }

    public OrderInfoSyncResult(SyncStatus status, string? message = null) : base(status, message)
    {
    }
}