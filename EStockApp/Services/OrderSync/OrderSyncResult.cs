using EStockApp.Models;

namespace EStockApp.Services.OrderSync;

public class OrderSyncResult : SyncResult<OrderProductItemSyncModel>
{
    public OrderSyncResult(OrderProductItemSyncModel result) : base(result)
    {
    }
}