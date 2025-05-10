namespace EStockApp.Models;

public class ProductItemModel
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public string Category { get; set; } = null!;
    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public string ProductModel { get; set; } = null!;
    public string BrandName { get; set; } = null!;
    public string? Pack { get; set; }
    public string[] OrderCodes { get; set; } = null!;
    public int TotalCount { get; set; }

    public int StockCount { get; set; }
}
