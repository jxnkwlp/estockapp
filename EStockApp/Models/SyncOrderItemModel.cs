namespace EStockApp.Models;

public class SyncOrderItemModel
{
    public string Category { get; set; } = null!;
    public int ProductId { get; set; }
    public required string ProductCode { get; set; }
    public string ProductName { get; set; } = null!;
    public string ProductModel { get; set; } = null!;
    public string BrandName { get; set; } = null!;
    public string? Pack { get; set; }
    public string[] OrderCodes { get; set; } = null!;
    public int TotalCount { get; set; }
}
