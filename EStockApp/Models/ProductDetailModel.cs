namespace EStockApp.Models;

public class ProductDetailModel
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public string ProductModel { get; set; } = null!;
    public string BrandName { get; set; } = null!;
    public string? Pack { get; set; }
    public string Category { get; set; } = null!;
    public string? StockUnitName { get; set; }
    public decimal UnitPrice { get; set; }
}
