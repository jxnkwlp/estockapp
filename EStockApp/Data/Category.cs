namespace EStockApp.Data;

/// <summary>
///  分类
/// </summary>
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string ItemCount { get; set; } = null!;
}