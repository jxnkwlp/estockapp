using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EStockApp.Models;
using EStockApp.Services;
using EStockApp.Services.OrderSync;
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
    private ObservableCollection<string> _logs = new ObservableCollection<string>();

    [ObservableProperty]
    private bool _loadFromOrder = true;

    private readonly IDataStore _dataStore;
    private readonly IOrderHistorySync _orderHistorySync;

    public SyncWindowViewModel(IDataStore dataStore, IOrderHistorySync orderHistorySync)
    {
        _dataStore = dataStore;
        _orderHistorySync = orderHistorySync;
    }

    [RelayCommand(CanExecute = nameof(CanStart), AllowConcurrentExecutions = false)]
    private async Task StartAsync()
    {
        IsBusy = true;

        if (!StartDate.HasValue)
            return;

        AddLogs("正在启动...");

        try
        {
            if (LoadFromOrder)
            {
                int count = 0;
                var result = _orderHistorySync.GetOrdersAsync(DateOnly.FromDateTime(StartDate.Value.Date));

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
                var result = _orderHistorySync.GetHistoriesAsync(DateOnly.FromDateTime(StartDate.Value.Date));
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

        foreach (var item in orderInfo.Products)
        {
            if (!await _dataStore.IsExistsAsync(item.ProductId))
            {
                await _dataStore.InsertAsync(item.ProductId, item.Category, item.ProductCode, item.ProductName, item.ProductModel, item.BrandName, item.Pack, item.StockUnitName, item.Price);
                await _dataStore.AddOrderMapAsync(item.ProductId, item.OrderNumber, item.Price, item.TotalCount, item.TotalCount);
                await _dataStore.AddCategoryAsync(item.Category);
            }
            else
            {
                await _dataStore.UpdateAsync(item.ProductId, item.Category, item.ProductCode, item.ProductName, item.ProductModel, item.BrandName, item.Pack, item.StockUnitName, item.Price);
                await _dataStore.AddOrderMapAsync(item.ProductId, item.OrderNumber, item.Price, item.TotalCount, item.TotalCount);
                await _dataStore.AddCategoryAsync(item.Category);
            }

            AddLogs($"订单({orderInfo.OrderNo}) 入库 {item.ProductCode}: {item.ProductName}，共{item.TotalCount}{item.StockUnitName}");
        }
    }

    private async Task AddOrUpdateProductAsync(OrderItemSyncModel item)
    {
        var orderId = item.OrderId;

        if (!await _dataStore.OrderExistsAsync(orderId))
        {
            var orderInfo = await _orderHistorySync.GetOrderAsync(orderId);
            if (orderInfo == null)
                return;

            await _dataStore.InsertOrderAsync(orderInfo.OrderId, orderInfo.OrderNo, orderInfo.TotalPrice, orderInfo.TotalDiscount, orderInfo.RealPrice, orderInfo.OrderTime, orderInfo.ItemsCount);

            AddLogs($"新增订单 {orderInfo.OrderNo}");
        }

        if (!await _dataStore.IsExistsAsync(item.ProductId))
        {
            await _dataStore.InsertAsync(item.ProductId, item.Category, item.ProductCode, item.ProductName, item.ProductModel, item.BrandName, item.Pack, item.StockUnitName, item.Price);
            await _dataStore.AddOrderMapAsync(item.ProductId, item.OrderNumber, item.Price, item.TotalCount, item.TotalCount);
            await _dataStore.AddCategoryAsync(item.Category);
        }
        else
        {
            await _dataStore.UpdateAsync(item.ProductId, item.Category, item.ProductCode, item.ProductName, item.ProductModel, item.BrandName, item.Pack, item.StockUnitName, item.Price);
            await _dataStore.AddOrderMapAsync(item.ProductId, item.OrderNumber, item.Price, item.TotalCount, item.TotalCount);
            await _dataStore.AddCategoryAsync(item.Category);
        }

        AddLogs($"订单({item.OrderNumber}) 入库 {item.ProductCode}: {item.ProductName}，共{item.TotalCount}{item.StockUnitName}");
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
