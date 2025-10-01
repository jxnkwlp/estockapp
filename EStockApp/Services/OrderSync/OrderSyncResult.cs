using EStockApp.Models;

namespace EStockApp.Services.OrderSync;

public class OrderSyncResult : SyncResult<OrderSyncModel>
{
    public OrderSyncResult()
    {
    }

    public OrderSyncResult(string error) : base(error)
    {
    }

    public OrderSyncResult(OrderSyncModel result) : base(result)
    {
    }
}