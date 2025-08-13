using EStockApp.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EStockApp.Services;

public interface IDataStore
{
    Task<List<Product>> GetListAsync(int maxResultCount, int skipCount, string? category = null, string? filter = null);

    Task<int> GetCountAsync(string? category = null, string? filter = null);

    Task<int> GetStockCountAsync(string? category = null, string? filter = null);

    Task<bool> IncreaseStockAsync(int id, int value);
    Task<bool> SetStockAsync(int id, int value);

    Task<bool> IsExistsAsync(int productId);

    Task<int> InsertAsync(Product item);
    Task<bool> UpdateAsync(Product item);
    Task DeleteAsync(int id);

    Task<Product?> GetAsync(int id);

    Task AddOrUpdateAsync(int productId, string orderCode, Product item);

    Task BackupAsync();

    Task<IReadOnlyList<string>> GetCategoryListAsync();

    Task<bool> OrderExistsAsync(string orderId);
    Task AddOrUpdateOrderAsync(Order order);
    Task<List<Order>> GetOrderListAsync();
}
