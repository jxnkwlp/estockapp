using EStockApp.Models;

namespace EStockApp.Services.OrderSync;

public class OrderHistorySyncResult
{
    public OrderHistorySyncResult(OrderHistorySyncStatus status, string? message = null)
    {
        Status = status;
        Message = message;
    }

    public OrderHistorySyncStatus Status { get; } = OrderHistorySyncStatus.Success;
    public string? Message { get; }
    public SyncOrderItemModel? OrderItem { get; set; }
}
