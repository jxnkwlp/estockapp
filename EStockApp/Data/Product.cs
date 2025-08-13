namespace EStockApp.Data;

public class Product
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

    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }

    public string? StockUnitName { get; set; }

    public int TotalCount { get; set; }

    public int StockCount { get; set; }
}
