using EStockApp.Data;
using LiteDB;
using LiteDB.Async;
using LiteDB.Queryable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EStockApp.Services;

public class LocalDataStore : IDataStore, IDisposable
{
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
    }

    private ILiteCollectionAsync<Product> GetProducts()
    {
        return _db.GetCollection<Product>("Products");
    }

    private ILiteCollectionAsync<Order> GetOrder()
    {
        return _db.GetCollection<Order>("Orders");
    }

    public async Task DeleteAsync(int id)
    {
        var collection = GetProducts();

        await collection.DeleteAsync(id);
    }

    public async Task<Product?> GetAsync(int id)
    {
        var collection = GetProducts();

        return await (collection.FindByIdAsync(id));
    }

    public async Task<int> GetCountAsync(string? category = null, string? filter = null)
    {
        var collection = GetProducts();

        var query = collection.AsQueryable();

        if (!string.IsNullOrEmpty(filter))
        {
            query = query.Where(x => x.ProductCode.Contains(filter) || x.ProductName.Contains(filter) || x.BrandName.Contains(filter));
        }
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(x => x.Category == category);
        }

        return await query.CountAsync();
    }


    public async Task<int> GetStockCountAsync(string? category = null, string? filter = null)
    {
        var collection = GetProducts();

        var query = collection.AsQueryable();

        if (!string.IsNullOrEmpty(filter))
        {
            query = query.Where(x => x.ProductCode.Contains(filter) || x.ProductName.Contains(filter) || x.BrandName.Contains(filter));
        }
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(x => x.Category == category);
        }

        return await query.SumAsync(x => x.StockCount);
    }

    public async Task<List<Product>> GetListAsync(int maxResultCount, int skipCount, string? category = null, string? filter = null)
    {
        var collection = GetProducts();

        var query = collection.AsQueryable();

        if (!string.IsNullOrEmpty(filter))
        {
            query = query.Where(x => x.ProductCode.Contains(filter) || x.ProductName.Contains(filter) || x.BrandName.Contains(filter));
        }
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(x => x.Category == category);
        }

        return await query.OrderBy(x => x.Category).Skip(skipCount).Take(maxResultCount).ToListAsync();
    }

    public async Task<bool> IncreaseStockAsync(int id, int value)
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

        var result = await collection.UpdateAsync(item);
        return result;
    }

    public async Task<int> InsertAsync(Product item)
    {
        var collection = GetProducts();

        var result = await collection.InsertAsync(item);
        return result.AsInt32;
    }

    public Task<bool> IsExistsAsync(int productId)
    {
        var collection = GetProducts();

        var result = collection.ExistsAsync(x => x.ProductId == productId);
        return result;
    }

    public async Task<bool> SetStockAsync(int id, int value)
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

        var result = await collection.UpdateAsync(item);
        return result;
    }

    public async Task<bool> UpdateAsync(Product item)
    {
        var collection = GetProducts();

        var model = await collection.FindByIdAsync(item.Id);
        if (model == null)
        {
            throw new Exception("不存在的数据");
        }

        model.BrandName = item.BrandName;
        model.OrderCodes = item.OrderCodes;
        model.Category = item.Category;
        model.Pack = item.Pack;
        model.ProductCode = item.ProductCode;
        model.ProductModel = item.ProductModel;
        model.ProductName = item.ProductName;
        model.TotalCount = item.TotalCount;
        model.StockCount = item.StockCount;

        var result = await collection.UpdateAsync(item);
        return result;
    }

    public async Task AddOrUpdateAsync(int productId, string orderCode, Product item)
    {
        var collection = GetProducts();

        var model = await collection.FindOneAsync(x => x.ProductId == productId);
        if (model == null)
        {
            item.StockCount = item.TotalCount;
            await InsertAsync(item);
        }
        else
        {
            model.BrandName = item.BrandName;
            model.Category = item.Category;
            model.Pack = item.Pack;
            model.ProductCode = item.ProductCode;
            model.ProductModel = item.ProductModel;
            model.ProductName = item.ProductName;
            model.TotalCount = item.TotalCount;
            model.StockUnitName = item.StockUnitName;

            model.UnitPrice = item.UnitPrice;

            if (!model.OrderCodes.Contains(orderCode))
            {
                model.OrderCodes = model.OrderCodes.Concat(new[] { orderCode }).Distinct().ToArray();

                model.TotalPrice += item.TotalPrice;
            }

            await collection.UpdateAsync(item);
        }
    }

    public Task<IReadOnlyList<string>> GetCategoryListAsync()
    {
        var collection = GetProducts();

        var list = collection.AsQueryable()
            .Select(x => x.Category)
            .ToList()
            .Distinct()
            .Order()
            .ToArray();

        return Task.FromResult<IReadOnlyList<string>>(list);
    }

    public async Task BackupAsync()
    {
        await ReIndexAsync();

        await _db.CheckpointAsync();

        await _db.RebuildAsync();

        await Task.Delay(500);

        // File.Copy("db.data", $"db_{DateTime.Now.ToString("yyyyMMddHHmmss")}.data");
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    public async Task AddOrUpdateOrderAsync(Order order)
    {
        var collection = GetOrder();

        var exist = await collection.FindOneAsync(x => x.OrderId == order.OrderId);

        if (exist != null)
        {
            exist.OrderNo = order.OrderNo;
            exist.OrderId = order.OrderId;
            exist.RealPrice = order.RealPrice;
            exist.TotalPrice = order.TotalPrice;
            exist.TotalDiscount = order.TotalDiscount;
            exist.OrderTime = order.OrderTime;
            exist.ItemsCount = order.ItemsCount;

            await collection.UpdateAsync(exist);
        }
        else
        {
            order.Id = 0;
            await collection.InsertAsync(order);
        }
    }

    public async Task<List<Order>> GetOrderListAsync()
    {
        var collection = GetOrder();

        var list = await collection.Query().OrderByDescending(x => x.OrderTime).ToListAsync();

        return list.ToList();
    }

    public async Task<bool> OrderExistsAsync(string orderId)
    {
        var collection = GetOrder();

        return await collection.ExistsAsync(x => x.OrderId == orderId);
    }
}