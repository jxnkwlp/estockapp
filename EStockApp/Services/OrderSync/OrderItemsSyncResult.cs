using EStockApp.Models;

namespace EStockApp.Services.OrderSync;

public class OrderItemsSyncResult : SyncResult<OrderItemSyncModel>
{
    public OrderItemsSyncResult()
    {
    }

    public OrderItemsSyncResult(string error) : base(error)
    {
    }

    public OrderItemsSyncResult(OrderItemSyncModel result) : base(result)
    {
    }
}
