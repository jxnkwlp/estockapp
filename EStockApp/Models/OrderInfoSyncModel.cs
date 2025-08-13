using System;
using System.Collections.Generic;

namespace EStockApp.Models;

public class OrderInfoSyncModel
{
    public string OrderId { get; set; } = null!;
    public string OrderNo { get; set; } = null!;
    public decimal TotalPrice { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal RealPrice { get; set; }
    public DateTime OrderTime { get; set; }
    public int ItemsCount { get; set; }

    public List<OrderProductItemSyncModel> Products { get; set; } = new List<OrderProductItemSyncModel>();
}