using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EStockApp.Models;
using EStockApp.Services;
using EStockApp.Services.RemoteApi;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace EStockApp.ViewModels;

public partial class SyncWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _maxYear = DateTime.Now.Year;

    [ObservableProperty]
    private DateTimeOffset? _startDate = DateTimeOffset.Now.AddMonths(-2);

    [ObservableProperty]
    private ObservableCollection<SyncRecordLine> _records = [];

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private bool _loadFromOrder = true;

    private readonly IDataStore _dataStore;
    private readonly IRemoteApi _remoteApi;

    public SyncWindowViewModel(IDataStore dataStore, IRemoteApi remoteApi)
    {
        _dataStore = dataStore;
        _remoteApi = remoteApi;
    }

    [RelayCommand(CanExecute = nameof(CanStart), AllowConcurrentExecutions = false)]
    private async Task StartAsync()
    {
        if (!StartDate.HasValue)
            return;

        IsBusy = true;
        Records.Clear();
        SetStatus("正在初始化...");

        try
        {
            if (LoadFromOrder)
            {
                int count = 0;
                SetStatus("读取数据中...");
                var result = _remoteApi.GetOrdersAsync(DateOnly.FromDateTime(StartDate.Value.Date));

                await foreach (var item in result)
                {
                    if (!string.IsNullOrWhiteSpace(item.Error))
                        SetStatus(item.Error);
                    else if (item.Result != null)
                    {
                        SetStatus($"正在同步订单 {item.Result.OrderNo}");
                        await AddOrUpdateOrderAndItemsAsync(item.Result);
                        count++;
                    }
                }

                SetStatus($"已同步 {count} 个订单");
            }
            else
            {
                SetStatus("读取数据中...");
                var result = _remoteApi.GetHistoriesAsync(DateOnly.FromDateTime(StartDate.Value.Date));
                int count = 0;

                await foreach (var item in result)
                {
                    if (!string.IsNullOrWhiteSpace(item.Error))
                        SetStatus(item.Error);
                    else if (item.Result != null)
                    {
                        SetStatus($"正在同步订单 {item.Result.OrderNumber}");
                        await AddOrUpdateProductAsync(item.Result);
                        count++;
                    }
                }

                SetStatus($"已更新 {count} 个器件");
            }

            SetStatus("同步完成");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }

        IsBusy = false;
    }

    private async Task AddOrUpdateOrderAndItemsAsync(OrderSyncModel orderInfo)
    {
        if (!await _dataStore.OrderExistsAsync(orderInfo.OrderId))
        {
            await _dataStore.InsertOrderAsync(orderInfo.OrderId, orderInfo.OrderNo, orderInfo.TotalPrice, orderInfo.TotalDiscount, orderInfo.RealPrice, orderInfo.OrderTime, orderInfo.ItemsCount);
        }

        foreach (var item in orderInfo.Products)
        {
            var isNewProduct = !await _dataStore.IsProductExistsAsync(item.ProductId);

            if (isNewProduct)
            {
                await _dataStore.InsertProductAsync(item.ProductId, item.Category, item.ProductCode, item.ProductName, item.ProductModel, item.BrandName, item.Pack, item.StockUnitName, item.Price);
                await _dataStore.AddProductOrderMapAsync(item.ProductId, item.OrderNumber, item.Price, item.TotalCount, item.TotalPrice, item.Discount);
                await _dataStore.AddCategoryAsync(item.Category);
                await _dataStore.AddBrandAsync(item.BrandName);
            }
            else
            {
                await _dataStore.UpdateProductAsync(item.ProductId, item.Category, item.ProductCode, item.ProductName, item.ProductModel, item.BrandName, item.Pack, item.StockUnitName, item.Price);
                await _dataStore.AddProductOrderMapAsync(item.ProductId, item.OrderNumber, item.Price, item.TotalCount, item.TotalPrice, item.Discount);
                await _dataStore.AddCategoryAsync(item.Category);
                await _dataStore.AddBrandAsync(item.BrandName);
            }

            AddRecord(new SyncRecordLine
            {
                ActionText = isNewProduct ? "新增" : "更新",
                OrderNo = orderInfo.OrderNo,
                ProductCode = item.ProductCode,
                ProductName = item.ProductName,
                Quantity = item.TotalCount,
                SyncedAt = DateTime.Now
            });
        }
    }

    private async Task AddOrUpdateProductAsync(OrderItemSyncModel item)
    {
        var orderId = item.OrderId;

        if (!await _dataStore.OrderExistsAsync(orderId))
        {
            var orderInfo = await _remoteApi.GetOrderAsync(orderId);
            if (orderInfo == null)
                return;

            await _dataStore.InsertOrderAsync(orderInfo.OrderId, orderInfo.OrderNo, orderInfo.TotalPrice, orderInfo.TotalDiscount, orderInfo.RealPrice, orderInfo.OrderTime, orderInfo.ItemsCount);
        }

        var isNewProduct = !await _dataStore.IsProductExistsAsync(item.ProductId);

        if (isNewProduct)
        {
            await _dataStore.InsertProductAsync(item.ProductId, item.Category, item.ProductCode, item.ProductName, item.ProductModel, item.BrandName, item.Pack, item.StockUnitName, item.Price);
            await _dataStore.AddProductOrderMapAsync(item.ProductId, item.OrderNumber, item.Price, item.TotalCount, item.TotalPrice, item.Discount);
            await _dataStore.AddCategoryAsync(item.Category);
            await _dataStore.AddBrandAsync(item.BrandName);
        }
        else
        {
            await _dataStore.UpdateProductAsync(item.ProductId, item.Category, item.ProductCode, item.ProductName, item.ProductModel, item.BrandName, item.Pack, item.StockUnitName, item.Price);
            await _dataStore.AddProductOrderMapAsync(item.ProductId, item.OrderNumber, item.Price, item.TotalCount, item.TotalPrice, item.Discount);
            await _dataStore.AddCategoryAsync(item.Category);
            await _dataStore.AddBrandAsync(item.BrandName);
        }

        AddRecord(new SyncRecordLine
        {
            ActionText = isNewProduct ? "新增" : "更新",
            OrderNo = item.OrderNumber,
            ProductCode = item.ProductCode,
            ProductName = item.ProductName,
            Quantity = item.TotalCount,
            SyncedAt = DateTime.Now
        });
    }

    private bool CanStart()
    {
        return StartDate.HasValue && StartDate.Value.Date <= DateTimeOffset.Now.Date;
    }

    private void SetStatus(string text)
    {
        StatusText = text;
    }

    private void AddRecord(SyncRecordLine line)
    {
        Records.Add(line);
    }
}
