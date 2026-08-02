using EStockApp.Models;

namespace EStockApp.Services.RemoteApi;

public class OrderItemsRemoteResult : RemoteResult<OrderItemSyncModel>
{
    public OrderItemsRemoteResult()
    {
    }

    public OrderItemsRemoteResult(string error) : base(error)
    {
    }

    public OrderItemsRemoteResult(OrderItemSyncModel result) : base(result)
    {
    }
}
