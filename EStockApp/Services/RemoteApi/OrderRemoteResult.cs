using EStockApp.Models;

namespace EStockApp.Services.RemoteApi;

public class OrderRemoteResult : RemoteResult<OrderSyncModel>
{
    public OrderRemoteResult()
    {
    }

    public OrderRemoteResult(string error) : base(error)
    {
    }

    public OrderRemoteResult(OrderSyncModel result) : base(result)
    {
    }
}
