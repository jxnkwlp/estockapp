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

    private readonly IDataStore _dataStore;

    public SyncWindowViewModel(IDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    [RelayCommand(CanExecute = nameof(CanStart), AllowConcurrentExecutions = false)]
    private async Task StartAsync()
    {
        IsBusy = true;

        if (!StartDate.HasValue)
            return;

        AddLogs("正在启动...");

        var result = OrderHistorySync.RunAsync(DateOnly.FromDateTime(StartDate.Value.Date));

        bool needLogin = false;

        await foreach (OrderHistorySyncResult item in result)
        {
            if (!string.IsNullOrWhiteSpace(item.Message))
                AddLogs(item.Message);

            if (item.Status == OrderHistorySyncStatus.RequreLogin)
            {
                needLogin = true;

                break;
            }
            else if (item.Status == OrderHistorySyncStatus.Success && item.OrderItem != null)
            {
                await AddOrUpdateOrderItem(item.OrderItem);
            }
        }

        if (needLogin)
        {
            AddLogs("正在打开登录窗口...");

            try
            {
                await OrderHistorySync.ShowLoginAsync();
                AddLogs("已登录");

                await StartAsync();
            }
            catch (Exception ex)
            {
                AddLogs($"错误：{ex.Message}");
            }
        }

        IsBusy = false;
    }

    private async Task AddOrUpdateOrderItem(SyncOrderItemModel item)
    {
        if (!await _dataStore.IsExistsAsync(item.ProductId))
        {
            await _dataStore.InsertAsync(new ProductItemModel()
            {
                ProductId = item.ProductId,
                BrandName = item.BrandName,
                OrderCodes = item.OrderCodes,
                Category = item.Category,
                Pack = item.Pack,
                ProductCode = item.ProductCode,
                ProductModel = item.ProductModel,
                ProductName = item.ProductName,
                TotalCount = item.TotalCount,
                StockCount = item.TotalCount,
            });
        }
        else
        {
            await _dataStore.AddOrUpdateAsync(item.ProductId, new ProductItemModel()
            {
                BrandName = item.BrandName,
                OrderCodes = item.OrderCodes,
                Category = item.Category,
                Pack = item.Pack,
                ProductCode = item.ProductCode,
                ProductModel = item.ProductModel,
                ProductName = item.ProductName,
                TotalCount = item.TotalCount,
            });
        }

        AddLogs($"已同步 {item.Category} {item.ProductName}");
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
