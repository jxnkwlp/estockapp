using System;

namespace EStockApp.Data;

public class Order
{
    public int Id { get; set; }
    public string OrderId { get; set; } = null!;
    public string OrderNo { get; set; } = null!;
    public decimal TotalPrice { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal RealPrice { get; set; }
    public DateTime OrderTime { get; set; }
    public int ItemsCount { get; set; }
}