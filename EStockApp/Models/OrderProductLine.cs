namespace EStockApp.Models;

public class OrderProductLine
{
    public string? BrandName { get; set; }
    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public string? ProductModel { get; set; }
    public int TotalCount { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal Discount { get; set; }
}
