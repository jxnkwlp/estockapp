using System;

namespace EStockApp.Data;

/// <summary>
///  订单记录
/// </summary>
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

/// <summary>
///  分类
/// </summary>
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string ItemCount { get; set; } = null!;
}