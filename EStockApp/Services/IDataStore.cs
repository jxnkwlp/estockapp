using EStockApp.Data;
using EStockApp.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EStockApp.Services;

public interface IDataStore
{
    Task RebuildAsync();

    Task<List<Product>> GetProductListAsync(int maxResultCount, int skipCount, string? category = null, string? filter = null);

    Task<Product?> GetProductAsync(int id);

    Task<int> GetCategoryCountAsync();

    Task<int> GetProductCountAsync(string? category = null, string? filter = null);
    Task<int> GetTotalCountAsync(string? category = null, string? filter = null);
    Task<int> GetStockCountAsync(string? category = null, string? filter = null);

    Task<bool> IncreaseProductStockAsync(int id, int value);
    Task<bool> SetProductStockAsync(int id, int value);

    Task<bool> IsProductExistsAsync(int productId);

    Task<int> InsertProductAsync(int productId, string category, string productCode, string productName, string? productModel, string? brandName, string? pack, string? stockUnitName, decimal unitPrice);

    Task<bool> UpdateProductAsync(int productId, string category, string productCode, string productName, string? productModel, string? brandName, string? pack, string? stockUnitName, decimal unitPrice);

    Task UpdateProductTotalCountAsync(int productId, int totalCount);

    Task<bool> AddProductOrderMapAsync(int productId, string orderNo, decimal unitPrice, int totalCount, decimal totalPrice, decimal discount = 0);

    Task DeleteProductAsync(int id);

    Task BackupAsync();

    Task<IReadOnlyList<string>> GetCategoryListAsync();
    Task<bool> AddCategoryAsync(string name);

    Task<IReadOnlyList<string>> GetBrandListAsync();
    Task<List<BrandListItem>> GetBrandListItemsAsync();
    Task<bool> AddBrandAsync(string? name);
    Task MigrateBrandsFromProductsAsync();
    Task<List<Product>> GetProductsByBrandAsync(string brandName);
    Task<List<BrandOrderLine>> GetOrderMapsByBrandAsync(string brandName);
    Task<List<OrderProductLine>> GetProductsByOrderNoAsync(string orderNo);

    Task<bool> OrderExistsAsync(string orderId);

    Task<int> InsertOrderAsync(string orderId, string orderNo, decimal totalPrice, decimal totalDiscount, decimal realPrice, DateTime orderTime, int itemsCount);

    Task<List<Order>> GetOrderListAsync();

    Task InitProductUsedCountAsync();

    Task MigrateOrderMapTotalPricesAsync();

    Task<bool> IsMigrationAppliedAsync(string migrationId);
    Task MarkMigrationAppliedAsync(string migrationId);

    Task<string?> GetSettingValueAsync(string key);
    Task SetSettingValueAsync(string key, string? value = null);
}
