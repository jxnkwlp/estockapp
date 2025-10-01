using EStockApp.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EStockApp.Services;

public interface IDataStore
{
    Task<List<Product>> GetListAsync(int maxResultCount, int skipCount, string? category = null, string? filter = null);

    Task<Product?> GetAsync(int id);

    Task<int> GetCategoryCountAsync();
    Task<int> GetTotalCountAsync(string? category = null, string? filter = null);
    Task<int> GetStockCountAsync(string? category = null, string? filter = null);

    Task<bool> IncreaseStockAsync(int id, int value);
    Task<bool> SetStockAsync(int id, int value);

    Task<bool> IsExistsAsync(int productId);

    Task<int> InsertAsync(int productId, string category, string productCode, string productName, string? productModel, string? brandName, string? pack, string? stockUnitName, decimal unitPrice);

    Task<bool> UpdateAsync(int productId, string category, string productCode, string productName, string? productModel, string? brandName, string? pack, string? stockUnitName, decimal unitPrice);

    Task UpdateTotalCountAsync(int productId, int totalCount);

    Task<bool> AddOrderMapAsync(int productId, string orderNo, decimal unitPrice, int totalCount, decimal totalPrice);

    Task DeleteAsync(int id);

    // Task AddOrUpdateAsync(int productId, string orderCode, Product item);

    Task BackupAsync();

    Task<IReadOnlyList<string>> GetCategoryListAsync();
    Task<bool> AddCategoryAsync(string name);
     
    Task<bool> OrderExistsAsync(string orderId);

    Task<int> InsertOrderAsync(string orderId, string orderNo, decimal totalPrice, decimal totalDiscount, decimal realPrice, DateTime orderTime, int itemsCount);

    Task<List<Order>> GetOrderListAsync();
}
