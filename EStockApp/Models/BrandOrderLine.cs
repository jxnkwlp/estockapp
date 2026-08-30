using System;

namespace EStockApp.Models;

public class BrandOrderLine
{
    public string OrderCode { get; set; } = null!;
    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public int TotalCount { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal Discount { get; set; }
    public DateTime? SyncedAt { get; set; }
}
