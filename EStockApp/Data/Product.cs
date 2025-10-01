using System.Collections.Generic;

namespace EStockApp.Data;

/// <summary>
///  产品
/// </summary>
public class Product
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string Category { get; set; } = null!;
    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public string? ProductModel { get; set; } = null!;
    public string? BrandName { get; set; } = null!;
    public string? Pack { get; set; }
    public string? StockUnitName { get; set; }

    /// <summary>
    ///  累计金额
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    ///  单价
    /// </summary>
    public decimal UnitPrice { get; set; }
    /// <summary>
    ///  合计总数
    /// </summary>
    public int TotalCount { get; set; }
    /// <summary>
    ///  剩余库存
    /// </summary>
    public int StockCount { get; set; }

    public List<ProductOrderMap> OrderMaps { get; set; } = new List<ProductOrderMap>();
}

public class ProductOrderMap
{
    public string OrderCode { get; set; } = null!;
    public decimal UnitPrice { get; set; }
    public int TotalCount { get; set; }
    public decimal TotalPrice { get; set; }
}