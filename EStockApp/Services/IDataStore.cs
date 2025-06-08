using EStockApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EStockApp.Services;

public interface IDataStore
{
    Task<List<ProductItemModel>> GetListAsync(int maxResultCount, int skipCount, string? category = null, string? filter = null);

    Task<int> GetCountAsync(string? category = null, string? filter = null);

    Task<int> GetStockCountAsync(string? category = null, string? filter = null);

    Task<bool> IncreaseStockAsync(int id, int value);
    Task<bool> SetStockAsync(int id, int value);

    Task<bool> IsExistsAsync(int productId);

    Task<int> InsertAsync(ProductItemModel item);
    Task<bool> UpdateAsync(ProductItemModel item);
    Task DeleteAsync(int id);

    Task<ProductItemModel?> GetAsync(int id);

    Task AddOrUpdateAsync(int productId, ProductItemModel item);

    Task<IReadOnlyList<string>> GetCategoryListAsync();
    Task BackupAsync();
}
