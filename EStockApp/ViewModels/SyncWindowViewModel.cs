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
    private ObservableCollection<string> _logs = [];

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
        IsBusy = true;

        if (!StartDate.HasValue)
            return;

        AddLogs("正在初始化...");

        try
        {
            if (LoadFromOrder)
            {
                int count = 0;
                var result = _remoteApi.GetOrdersAsync(DateOnly.FromDateTime(StartDate.Value.Date));

                await foreach (var item in result)
                {
                    if (!string.IsNullOrWhiteSpace(item.Error))
                        AddLogs(item.Error);
                    else if (item.Result != null)
                    {
                        await AddOrUpdateOrderAndItemsAsync(item.Result);
                        count++;
                    }
                }

                AddLogs($"已同步 {count} 个订单");
            }
            else
            {
                var result = _remoteApi.GetHistoriesAsync(DateOnly.FromDateTime(StartDate.Value.Date));
                int count = 0;

                await foreach (var item in result)
                {
                    if (!string.IsNullOrWhiteSpace(item.Error))
                        AddLogs(item.Error);
                    else if (item.Result != null)
                    {
                        await AddOrUpdateProductAsync(item.Result);
                        count++;
                    }
                }

                AddLogs($"已更新 {count} 个器件");
            }

            AddLogs("同步完成！");
        }
        catch (Exception ex)
        {
            AddLogs(ex.Message);
        }

        IsBusy = false;
    }

    private async Task AddOrUpdateOrderAndItemsAsync(OrderSyncModel orderInfo)
    {
        if (!await _dataStore.OrderExistsAsync(orderInfo.OrderId))
        {
            await _dataStore.InsertOrderAsync(orderInfo.OrderId, orderInfo.OrderNo, orderInfo.TotalPrice, orderInfo.TotalDiscount, orderInfo.RealPrice, orderInfo.OrderTime, orderInfo.ItemsCount);

            AddLogs($"新增订单 {orderInfo.OrderNo}");
        }
        else
        {
            AddLogs($"订单 {orderInfo.OrderNo} 已存在");
        }

        foreach (var item in orderInfo.Products)
        {
            if (!await _dataStore.IsProductExistsAsync(item.ProductId))
            {
                await _dataStore.InsertProductAsync(item.ProductId, item.Category, item.ProductCode, item.ProductName, item.ProductModel, item.BrandName, item.Pack, item.StockUnitName, item.Price);
                await _dataStore.AddProductOrderMapAsync(item.ProductId, item.OrderNumber, item.Price, item.TotalCount, item.TotalPrice, item.Discount);
                await _dataStore.AddCategoryAsync(item.Category);
            }
            else
            {
                await _dataStore.UpdateProductAsync(item.ProductId, item.Category, item.ProductCode, item.ProductName, item.ProductModel, item.BrandName, item.Pack, item.StockUnitName, item.Price);
                await _dataStore.AddProductOrderMapAsync(item.ProductId, item.OrderNumber, item.Price, item.TotalCount, item.TotalPrice, item.Discount);
                await _dataStore.AddCategoryAsync(item.Category);
            }

            AddLogs($"订单({orderInfo.OrderNo})更新器件 {item.ProductCode}: {item.ProductName}，共{item.TotalCount}{item.StockUnitName}");
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

            AddLogs($"新增订单 {orderInfo.OrderNo}");
        }
        else
        {
            AddLogs($"订单 {orderId} 已存在");
        }

        if (!await _dataStore.IsProductExistsAsync(item.ProductId))
        {
            await _dataStore.InsertProductAsync(item.ProductId, item.Category, item.ProductCode, item.ProductName, item.ProductModel, item.BrandName, item.Pack, item.StockUnitName, item.Price);
            await _dataStore.AddProductOrderMapAsync(item.ProductId, item.OrderNumber, item.Price, item.TotalCount, item.TotalPrice, item.Discount);
            await _dataStore.AddCategoryAsync(item.Category);
        }
        else
        {
            await _dataStore.UpdateProductAsync(item.ProductId, item.Category, item.ProductCode, item.ProductName, item.ProductModel, item.BrandName, item.Pack, item.StockUnitName, item.Price);
            await _dataStore.AddProductOrderMapAsync(item.ProductId, item.OrderNumber, item.Price, item.TotalCount, item.TotalPrice, item.Discount);
            await _dataStore.AddCategoryAsync(item.Category);
        }

        AddLogs($"订单({item.OrderNumber})更新器件 {item.ProductCode}: {item.ProductName}，共{item.TotalCount}{item.StockUnitName}");
    }

    private bool CanStart()
    {
        return StartDate.HasValue && StartDate.Value.Date <= DateTimeOffset.Now.Date;
    }

    private void AddLogs(string text)
    {
        Logs.Insert(0, $"[{DateTime.Now}] {text}");
    }
}
