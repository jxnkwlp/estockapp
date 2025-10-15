using EStockApp.Data;
using LiteDB;
using LiteDB.Async;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EStockApp.Services;

public class LocalDataStore : IDataStore, IDisposable
{
    private DateTime _lastCheckpoint = DateTime.MinValue;

    private LiteDatabaseAsync _db = null!;

    public LocalDataStore()
    {
        // Task.Factory.StartNew(() => ReIndexAsync());
    }

    public void Initial()
    {
        try
        {
#if DEBUG
            _db = new LiteDatabaseAsync("Filename=db.data;Connection=shared");
#else
        _db = new LiteDatabaseAsync("db.data");
#endif
            // _db.RebuildAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    protected async Task ReIndexAsync()
    {
        var collection = GetProducts();
        await collection.EnsureIndexAsync(x => x.Category);
        await collection.EnsureIndexAsync(x => x.Pack);
        await collection.EnsureIndexAsync(x => x.ProductCode);
        await collection.EnsureIndexAsync(x => x.ProductModel);
    }

    private ILiteCollectionAsync<Product> GetProducts()
    {
        return _db.GetCollection<Product>("Products");
    }

    private ILiteCollectionAsync<Order> GetOrder()
    {
        return _db.GetCollection<Order>("Orders");
    }

    private ILiteCollectionAsync<Category> GetCategory()
    {
        return _db.GetCollection<Category>("Categories");
    }

    private ILiteCollectionAsync<Setting> GetSettings()
    {
        return _db.GetCollection<Setting>("Settings");
    }

    private ILiteCollectionAsync<UsedSummary> GetUsedSummary()
    {
        return _db.GetCollection<UsedSummary>("UsedSummaries");
    }

    private ILiteCollectionAsync<UsedHistory> GetUsedHistory()
    {
        return _db.GetCollection<UsedHistory>("UsedHistories");
    }

    public async Task DeleteProductAsync(int id)
    {
        var collection = GetProducts();

        await collection.DeleteAsync(id);
    }

    public async Task RebuildAsync()
    {
        await AutoCheckpointAsync();
    }

    public async Task<Product?> GetProductAsync(int id)
    {
        var collection = GetProducts();

        return await (collection.FindByIdAsync(id));
    }

    public async Task<int> GetCategoryCountAsync()
    {
        var collection = GetCategory();

        var query = collection.Query();

        return await query.CountAsync();
    }

    public async Task<int> GetProductCountAsync()
    {
        var collection = GetProducts();

        return await collection.CountAsync();
    }

    public async Task<int> GetTotalCountAsync(string? category = null, string? filter = null)
    {
        var collection = GetProducts();

        var query = collection.Query();

        if (!string.IsNullOrEmpty(filter))
        {
            query = query.Where(x => x.ProductCode.Contains(filter) || x.ProductName.Contains(filter) || x.BrandName!.Contains(filter) || x.ProductModel!.Contains(filter));
        }
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(x => x.Category == category);
        }

        return (await query.Select(x => x.TotalCount).ToArrayAsync()).Sum();
    }


    public async Task<int> GetStockCountAsync(string? category = null, string? filter = null)
    {
        var collection = GetProducts();

        var query = collection.Query();

        if (!string.IsNullOrEmpty(filter))
        {
            query = query.Where(x => x.ProductCode.Contains(filter) || x.ProductName.Contains(filter) || x.BrandName!.Contains(filter) || x.ProductModel!.Contains(filter));
        }
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(x => x.Category == category);
        }

        return (await query.Select(x => x.StockCount).ToArrayAsync()).Sum();
    }

    public async Task<List<Product>> GetProductListAsync(int maxResultCount, int skipCount, string? category = null, string? filter = null)
    {
        var collection = GetProducts();

        var query = collection.Query();

        if (!string.IsNullOrEmpty(filter))
        {
            query = query.Where(x => x.ProductCode.Contains(filter) || x.ProductName.Contains(filter) || x.BrandName!.Contains(filter) || x.ProductModel!.Contains(filter));
        }
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(x => x.Category == category);
        }

        return await query.OrderBy(x => x.Category).Skip(skipCount).Limit(maxResultCount).ToListAsync();
    }

    public async Task<bool> IncreaseProductStockAsync(int id, int value)
    {
        var collection = GetProducts();

        var item = await collection.FindByIdAsync(id);
        if (item == null)
        {
            throw new Exception("不存在的数据");
        }

        item.StockCount += value;
        if (item.StockCount < 0)
        {
            item.StockCount = 0;
        }

        item.UsedCount = item.TotalCount - item.StockCount;

        var result = await collection.UpdateAsync(item);

        await AutoCheckpointAsync();

        await UpdateUsedAsync(item.ProductCode, item.UsedCount);

        return result;
    }

    public async Task<bool> SetProductStockAsync(int id, int value)
    {
        var collection = GetProducts();

        var item = await collection.FindByIdAsync(id);
        if (item == null)
        {
            throw new Exception("不存在的数据");
        }

        item.StockCount = value;
        if (item.StockCount < 0)
        {
            item.StockCount = 0;
        }

        item.UsedCount = item.TotalCount - item.StockCount;

        var result = await collection.UpdateAsync(item);

        await AutoCheckpointAsync();

        await UpdateUsedAsync(item.ProductCode, item.UsedCount);

        return result;
    }

    public async Task UpdateUsedAsync(string productCode, int value)
    {
        var summaries = GetUsedSummary();
        var history = GetUsedHistory();

        var exist = await summaries.FindOneAsync(x => x.ProductCode == productCode);

        if (exist != null)
        {
            exist.UsedCount = value;

            await summaries.UpdateAsync(exist);
        }
        else
        {
            exist = new UsedSummary
            {
                ProductCode = productCode,
                UsedCount = value
            };

            await summaries.InsertAsync(exist);
        }

        await history.InsertAsync(new UsedHistory() { ProductCode = productCode, UsedCount = value });

        await AutoCheckpointAsync();
    }

    public async Task<bool> IsProductExistsAsync(int productId)
    {
        var collection = GetProducts();

        var result = await collection.ExistsAsync(x => x.ProductId == productId);

        await AutoCheckpointAsync();

        return result;
    }

    public async Task<int> InsertProductAsync(int productId, string category, string productCode, string productName, string? productModel, string? brandName, string? pack, string? stockUnitName, decimal unitPrice)
    {
        var collection = GetProducts();

        var model = new Product();
        model.ProductId = productId;
        model.BrandName = brandName;
        model.Category = category;
        model.Pack = pack;
        model.ProductCode = productCode;
        model.ProductModel = productModel;
        model.ProductName = productName;
        model.StockUnitName = stockUnitName;
        model.UnitPrice = unitPrice;

        var result = await collection.InsertAsync(model);

        await AutoCheckpointAsync();

        return result;
    }

    public async Task<bool> UpdateProductAsync(int productId, string category, string productCode, string productName, string? productModel, string? brandName, string? pack, string? stockUnitName, decimal unitPrice)
    {
        var collection = GetProducts();

        var model = await collection.FindOneAsync(x => x.ProductId == productId);
        if (model == null)
        {
            throw new Exception("不存在的数据");
        }

        model.ProductId = productId;
        model.BrandName = brandName;
        model.Category = category;
        model.Pack = pack;
        model.ProductCode = productCode;
        model.ProductModel = productModel;
        model.ProductName = productName;
        model.StockUnitName = stockUnitName;
        model.UnitPrice = unitPrice;

        var result = await collection.UpdateAsync(model);

        await AutoCheckpointAsync();

        return result;
    }

    public async Task UpdateProductTotalCountAsync(int productId, int totalCount)
    {
        var collection = GetProducts();

        var model = await collection.FindOneAsync(x => x.ProductId == productId);
        if (model == null)
        {
            throw new Exception("不存在的数据");
        }

        model.TotalCount = totalCount;

        var result = await collection.UpdateAsync(model);

        await AutoCheckpointAsync();
    }

    public async Task<bool> AddProductOrderMapAsync(int productId, string orderNo, decimal unitPrice, int totalCount, decimal totalPrice)
    {
        var collection = GetProducts();

        var model = await collection.FindOneAsync(x => x.ProductId == productId);
        if (model == null)
        {
            throw new Exception("不存在的数据");
        }

        if (!model.OrderMaps.Any(x => x.OrderCode == orderNo))
        {
            model.OrderMaps.Add(new ProductOrderMap
            {
                OrderCode = orderNo,
                TotalCount = totalCount,
                TotalPrice = totalPrice,
                UnitPrice = unitPrice,
            });
        }

        // update product
        model.UnitPrice = unitPrice;
        model.TotalAmount = model.OrderMaps.Sum(x => x.TotalPrice);
        model.TotalCount = model.OrderMaps.Sum(x => x.TotalCount);
        model.StockCount = model.TotalCount - model.UsedCount;

        var result = await collection.UpdateAsync(model);

        await AutoCheckpointAsync();

        return result;
    }

    public async Task<IReadOnlyList<string>> GetCategoryListAsync()
    {
        var collection = GetCategory();

        var list = await collection.FindAllAsync();

        return list.Select(x => x.Name).OrderBy(x => x).ToList();
    }

    public async Task<bool> AddCategoryAsync(string name)
    {
        var collection = GetCategory();

        if (await collection.ExistsAsync(x => x.Name.ToLowerInvariant() == name.ToLowerInvariant()))
            return false;

        await collection.InsertAsync(new Category { Name = name });

        return true;
    }

    public async Task BackupAsync()
    {
        await ReIndexAsync();

        await _db.CheckpointAsync();

        await _db.RebuildAsync();

        await Task.Delay(500);

        // File.Copy("db.data", $"db_{DateTime.Now.ToString("yyyyMMddHHmmss")}.data");
    }

    protected async Task AutoCheckpointAsync()
    {
        if (_lastCheckpoint.AddSeconds(10) < DateTime.Now)
        {
            _lastCheckpoint = DateTime.Now;
            await _db.CheckpointAsync();
        }
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    public async Task<bool> OrderExistsAsync(string orderId)
    {
        var collection = GetOrder();

        return await collection.ExistsAsync(x => x.OrderId == orderId);
    }

    public async Task<int> InsertOrderAsync(string orderId, string orderNo, decimal totalPrice, decimal totalDiscount, decimal realPrice, DateTime orderTime, int itemsCount)
    {
        var collection = GetOrder();

        var result = await collection.InsertAsync(new Order
        {
            ItemsCount = itemsCount,
            OrderNo = orderNo,
            TotalPrice = totalPrice,
            TotalDiscount = totalDiscount,
            RealPrice = realPrice,
            OrderId = orderId,
            OrderTime = orderTime,
        });

        await AutoCheckpointAsync();

        return result.AsInt32;
    }

    public async Task<List<Order>> GetOrderListAsync()
    {
        var collection = GetOrder();

        var list = await collection.Query().OrderByDescending(x => x.OrderTime).ToListAsync();

        return list.ToList();
    }

    public async Task InitProductUsedCountAsync()
    {
        var collection = GetProducts();

        var list = (await collection.FindAllAsync()).ToArray();

        foreach (var item in list)
        {
            if (item.UsedCount == 0 && item.StockCount < item.TotalCount)
            {
                item.UsedCount = item.TotalCount - item.StockCount;

                await collection.UpdateAsync(item);
            }
        }
    }

    public async Task<string?> GetSettingValueAsync(string key)
    {
        var query = GetSettings();

        return (await query.FindOneAsync(x => x.Key == key))?.Value;
    }

    public async Task SetSettingValueAsync(string key, string? value = null)
    {
        var collection = GetSettings();

        var exist = await collection.FindOneAsync(x => x.Key == key);

        if (exist != null)
        {
            exist.Value = value;

            await collection.UpdateAsync(exist);
        }
        else
        {
            await collection.InsertAsync(new Setting
            {
                Key = key,
                Value = value
            });
        }
    }

}