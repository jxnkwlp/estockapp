using EStockApp.Models;
using LiteDB;
using LiteDB.Queryable;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EStockApp.Services;

public class LocalDataStore : IDataStore
{
    private readonly LiteDatabase _db;

    public LocalDataStore()
    {
#if DEBUG
        _db = new LiteDatabase("Filename=db.data;Connection=shared");
#else
        _db = new LiteDatabase("db.data");
#endif
        ReIndex();
    }

    protected void ReIndex()
    {
        var collection = GetProducts();
        collection.EnsureIndex(x => x.Category);
        collection.EnsureIndex(x => x.Pack);
    }

    private ILiteCollection<ProductItemModel> GetProducts()
    {
        return _db.GetCollection<ProductItemModel>("Products");
    }

    public Task DeleteAsync(int id)
    {
        var collection = GetProducts();

        collection.Delete(id);

        return Task.CompletedTask;
    }

    public Task<ProductItemModel?> GetAsync(int id)
    {
        var collection = GetProducts();

        return Task.FromResult<ProductItemModel?>(collection.FindById(id));
    }

    public Task<int> GetCountAsync(string? category = null, string? filter = null)
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

        return Task.FromResult(query.Count());
    }

    public async Task<List<ProductItemModel>> GetListAsync(int maxResultCount, int skipCount, string? category = null, string? filter = null)
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

    public Task<bool> IncreaseStockAsync(int id, int value)
    {
        var collection = GetProducts();

        var item = collection.FindById(id);
        if (item == null)
        {
            throw new Exception("不存在的数据");
        }

        item.StockCount += value;
        if (item.StockCount < 0)
        {
            item.StockCount = 0;
        }

        var result = collection.Update(item);
        return Task.FromResult(result);
    }

    public Task<int> InsertAsync(ProductItemModel item)
    {
        var collection = GetProducts();

        var result = collection.Insert(item);
        return Task.FromResult(result.AsInt32);
    }

    public Task<bool> IsExistsAsync(int productId)
    {
        var collection = GetProducts();

        var result = collection.Exists(x => x.ProductId == productId);
        return Task.FromResult(result);
    }

    public Task<bool> SetStockAsync(int id, int value)
    {
        var collection = GetProducts();

        var item = collection.FindById(id);
        if (item == null)
        {
            throw new Exception("不存在的数据");
        }

        item.StockCount = value;
        if (item.StockCount < 0)
        {
            item.StockCount = 0;
        }

        var result = collection.Update(item);
        return Task.FromResult(result);
    }

    public Task<bool> UpdateAsync(ProductItemModel item)
    {
        var collection = GetProducts();

        var model = collection.FindById(item.Id);
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

        var result = collection.Update(item);
        return Task.FromResult(result);
    }

    public async Task AddOrUpdateAsync(int productId, ProductItemModel item)
    {
        var collection = GetProducts();

        var model = collection.FindOne(x => x.ProductId == productId);
        if (model == null)
        {
            await InsertAsync(item);
        }
        else
        {
            model.BrandName = item.BrandName;
            model.OrderCodes = item.OrderCodes;
            model.Category = item.Category;
            model.Pack = item.Pack;
            model.ProductCode = item.ProductCode;
            model.ProductModel = item.ProductModel;
            model.ProductName = item.ProductName;
            model.TotalCount = item.TotalCount;

            collection.Update(item);
        }
    }

    public Task<IReadOnlyList<string>> GetCategoryListAsync()
    {
        var collection = GetProducts();

        var list = collection.Query()
            .Select(x => x.Category)
            .ToList()
            .Distinct()
            .Order()
            .ToArray();

        return Task.FromResult<IReadOnlyList<string>>(list);
    }

    public async Task BackupAsync()
    {
        _db.Checkpoint();
        _db.Commit();

        _db.Rebuild();

        await Task.Delay(500);

        File.Copy("db.data", $"db_{DateTime.Now.ToString("yyyyMMddHHmmss")}.data");
    }
}