namespace EStockApp.Models;

public class OrderItemSyncModel
{
    public string OrderId { get; set; } = null!;
    public string OrderNumber { get; set; } = null!;

    public string Category { get; set; } = null!;
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public string ProductModel { get; set; } = null!;
    public string BrandName { get; set; } = null!;
    public string? Pack { get; set; }

    // public DateTime OrderTime { get; set; }

    public int TotalCount { get; set; }
    public decimal Price { get; set; }
    public decimal TotalPrice { get; set; }
    public string? StockUnitName { get; set; }

    public override string ToString()
    {
        return $"{ProductId}({ProductCode}) {ProductName}: {TotalCount}";
    }
}
