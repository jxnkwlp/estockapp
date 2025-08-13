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
    private DateTimeOffset? _startDate = DateTimeOffset.Now.AddDays(-30);

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
                var result = _orderHistorySync.GetOrderListAsync(DateOnly.FromDateTime(StartDate.Value.Date));

                await foreach (var item in result)
                {
                    if (!string.IsNullOrWhiteSpace(item.Message))
                        AddLogs(item.Message);

                    if (item.Status == SyncStatus.Success && item.Result != null)
                    {
                        await AddOrUpdateOrderItemAsync(item.Result);
                        count++;
                    }
                }

                AddLogs($"已更新 {count} 个订单");
            }
            else
            {
                var result = _orderHistorySync.GetHistoriesAsync(DateOnly.FromDateTime(StartDate.Value.Date));
                int count = 0;

                await foreach (var item in result)
                {
                    if (!string.IsNullOrWhiteSpace(item.Message))
                        AddLogs(item.Message);

                    if (item.Status == SyncStatus.Success && item.Result != null)
                    {
                        await AddOrUpdateOrderProductAsync(item.Result);
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

    private async Task AddOrUpdateOrderItemAsync(OrderInfoSyncModel orderInfo)
    {
        await _dataStore.AddOrUpdateOrderAsync(new Data.Order
        {
            OrderId = orderInfo.OrderId,
            OrderNo = orderInfo.OrderNo,
            OrderTime = orderInfo.OrderTime,
            RealPrice = orderInfo.RealPrice,
            TotalDiscount = orderInfo.TotalDiscount,
            TotalPrice = orderInfo.TotalPrice,
            ItemsCount = orderInfo.ItemsCount, 
        });

        AddLogs($"已添加订单 {orderInfo.OrderNo}");

        foreach (var item in orderInfo.Products)
        {
            if (!await _dataStore.IsExistsAsync(item.ProductId))
            {
                await _dataStore.InsertAsync(new Data.Product()
                {
                    OrderCodes = new[] { item.OrderNumber },
                    ProductId = item.ProductId,
                    BrandName = item.BrandName,
                    Category = item.Category,
                    Pack = item.Pack,
                    ProductCode = item.ProductCode,
                    ProductModel = item.ProductModel,
                    ProductName = item.ProductName,
                    TotalCount = item.TotalCount,
                    StockCount = item.TotalCount,
                    TotalPrice = item.TotalPrice,
                    StockUnitName = item.StockUnitName,
                    UnitPrice = item.Price,
                });
            }
            else
            {
                await _dataStore.AddOrUpdateAsync(item.ProductId, item.OrderNumber, new Data.Product()
                {
                    ProductId = item.ProductId,
                    BrandName = item.BrandName,
                    Category = item.Category,
                    Pack = item.Pack,
                    ProductCode = item.ProductCode,
                    ProductModel = item.ProductModel,
                    ProductName = item.ProductName,
                    TotalCount = item.TotalCount,
                    StockCount = item.TotalCount,
                    TotalPrice = item.TotalPrice,
                    StockUnitName = item.StockUnitName,
                    UnitPrice = item.Price,
                });
            }
        }
    }

    private async Task AddOrUpdateOrderProductAsync(OrderProductItemSyncModel item)
    {
        if (!await _dataStore.IsExistsAsync(item.ProductId))
        {
            await _dataStore.InsertAsync(new Data.Product()
            {
                OrderCodes = new[] { item.OrderNumber },
                ProductId = item.ProductId,
                BrandName = item.BrandName,
                Category = item.Category,
                Pack = item.Pack,
                ProductCode = item.ProductCode,
                ProductModel = item.ProductModel,
                ProductName = item.ProductName,
                TotalCount = item.TotalCount,
                StockCount = item.TotalCount,
                TotalPrice = item.TotalPrice,
                StockUnitName = item.StockUnitName,
                UnitPrice = item.Price,
            });
        }
        else
        {
            await _dataStore.AddOrUpdateAsync(item.ProductId, item.OrderNumber, new Data.Product()
            {
                ProductId = item.ProductId,
                BrandName = item.BrandName,
                Category = item.Category,
                Pack = item.Pack,
                ProductCode = item.ProductCode,
                ProductModel = item.ProductModel,
                ProductName = item.ProductName,
                TotalCount = item.TotalCount,
                StockCount = item.TotalCount,
                TotalPrice = item.TotalPrice,
                StockUnitName = item.StockUnitName,
                UnitPrice = item.Price,
            });
        }

        AddLogs($"已同步 {item.Category} {item.ProductName}");

        var orderId = item.OrderId;
        if (!await _dataStore.OrderExistsAsync(orderId))
        {
            var orderInfo = await _orderHistorySync.GetOrderAsync(orderId);
            if (orderInfo == null)
                return;

            await _dataStore.AddOrUpdateOrderAsync(new Data.Order
            {
                OrderId = orderId,
                OrderNo = orderInfo.OrderNo,
                OrderTime = orderInfo.OrderTime,
                RealPrice = orderInfo.RealPrice,
                TotalDiscount = orderInfo.TotalDiscount,
                TotalPrice = orderInfo.TotalPrice,
            });

            AddLogs($"已同步订单 {orderInfo.OrderNo}");
        }
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
