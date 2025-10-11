using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EStockApp.Models;
using EStockApp.Services;
using EStockApp.Views;
using Microsoft.Extensions.DependencyInjection;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Nelibur.ObjectMapper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Notification = Avalonia.Controls.Notifications.Notification;
using WindowNotificationManager = Avalonia.Controls.Notifications.WindowNotificationManager;

namespace EStockApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    //[ObservableProperty]
    //private ISukiDialogManager _dialogManager = new SukiDialogManager();
    //[ObservableProperty]
    //private ISukiToastManager _toastManager = new SukiToastManager();

    [ObservableProperty]
    private string? _listFilter;

    [ObservableProperty]
    private ObservableCollection<ProductItemModel> _items = new ObservableCollection<ProductItemModel>();
    [ObservableProperty]
    private ObservableCollection<string> _categoryList = new ObservableCollection<string>(new string[] { "全部" });
    [ObservableProperty]
    private string? _selectCategory = "全部";

    [ObservableProperty]
    private int _totalCategoryCount;
    [ObservableProperty]
    private int _totalCount;
    [ObservableProperty]
    private int _totalStockCount;

    private readonly IDataStore _dataStore;
    private readonly WindowNotificationManager _notificationManager;
    private readonly DbMigration _dbMigration;

    public MainWindowViewModel(IDataStore dataStore, WindowNotificationManager notificationManager, DbMigration dbMigration)
    {
        _dataStore = dataStore;
        _notificationManager = notificationManager;
        _dbMigration = dbMigration;
    }

    public override async Task InitialAsync(Dictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
    {
        await _dataStore.RebuildAsync();

        await LoadSummaryAsync();
        await LoadCategory();
        await LoadList();

        var migration_251110 = await _dataStore.GetSettingValueAsync("migration_251110");
        if (string.IsNullOrWhiteSpace(migration_251110))
        {
            await _dbMigration.MigrateAsync();

            await _dataStore.SetSettingValueAsync("migration_251110", "1");
        }
    }

    [RelayCommand]
    private async Task ShowSyncViewAsync()
    {
        var historySyncWindow = App.ServiceProvider.GetRequiredService<SyncWindow>();
        historySyncWindow.DataContext = App.ServiceProvider.GetRequiredService<SyncWindowViewModel>();
        await historySyncWindow.ShowDialog(App.ServiceProvider.GetRequiredService<MainWindow>());

        ListFilter = null;
        SelectCategory = null;

        await LoadSummaryAsync();
        await LoadCategory();
        await LoadList();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        IsBusy = true;

        try
        {
            await LoadList();
        }
        catch (System.Exception ex)
        {
            _notificationManager.Show(new Notification("错误", ex.Message, NotificationType.Error));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadList()
    {
        try
        {
            var list = await _dataStore.GetProductListAsync(int.MaxValue, 0, SelectCategory == "全部" ? null : SelectCategory, ListFilter);
            Items.Clear();
            Items = new ObservableCollection<ProductItemModel>(list.Select(x => TinyMapper.Map<ProductItemModel>(x)));
        }
        catch (System.Exception ex)
        {
            _notificationManager.Show(new Notification("错误", ex.Message, NotificationType.Error));
        }
    }

    private async Task LoadSummaryAsync()
    {
        TotalCategoryCount = await _dataStore.GetProductCountAsync();
        TotalCount = await _dataStore.GetTotalCountAsync();
        TotalStockCount = await _dataStore.GetStockCountAsync();
    }

    private async Task LoadCategory()
    {
        var categoryList = (await _dataStore.GetCategoryListAsync()).ToList();
        categoryList.Insert(0, "全部");

        CategoryList.Clear();
        CategoryList = new ObservableCollection<string>(categoryList);

        if (!categoryList.Contains(SelectCategory!))
            SelectCategory = "全部";
    }

    [RelayCommand]
    private async Task UpdateStockAsync(int id)
    {
        var vm = App.ServiceProvider.GetRequiredService<StockEditViewModel>();

        await vm.InitialAsync();
        vm.SetId(id);

        await DialogHost.ShowDialogAsync(new StockEditView(), vm, new DialogOptions()
        {
            Title = "库存",
            CanResize = false,
        });

        await LoadList();
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var vm = App.ServiceProvider.GetRequiredService<ProductEditViewModel>();

        await vm.InitialAsync();

        await DialogHost.ShowDialogAsync(new ProductEditView(), vm, new DialogOptions()
        {
            Title = "新增",
            CanResize = false,
            Height = 600,
        });

        await LoadList();
    }

    [RelayCommand]
    private async Task EditAsync(int id)
    {
        var vm = App.ServiceProvider.GetRequiredService<ProductEditViewModel>();

        if (!await vm.LoadAsync(id))
        {
            return;
        }

        await vm.InitialAsync();

        await DialogHost.ShowDialogAsync(new ProductEditView(), vm, new DialogOptions()
        {
            Title = "编辑",
            CanResize = false,
            Height = 510,
        });

        await LoadList();
    }

    [RelayCommand]
    private async Task DeleteAsync(int id)
    {
        var item = await _dataStore.GetProductAsync(id);
        if (item == null)
        {
            _notificationManager.Show(new Notification("错误", "数据不存在", NotificationType.Error));
            return;
        }

        var box = MessageBoxManager.GetMessageBoxStandard("删除确认", $"{item.BrandName}\n{item.Category}\n{item.ProductName}\n{item.Pack}\n{item.ProductCode}", ButtonEnum.YesNo);

        if (await box.ShowWindowDialogAsync(App.ServiceProvider.GetRequiredService<MainWindow>()) == ButtonResult.Yes)
        {
            await _dataStore.DeleteProductAsync(id);
        }

        await LoadList();
    }

    [RelayCommand]
    private async Task ShowOrderNoAsync(int id)
    {
        var item = await _dataStore.GetProductAsync(id);
        if (item == null)
        {
            _notificationManager.Show(new Notification("错误", "数据不存在", NotificationType.Error));
            return;
        }

        if (item.OrderMaps.Any() != true)
        {
            await MessageBoxManager.GetMessageBoxStandard("提示", "无相关订单", ButtonEnum.Ok).ShowWindowDialogAsync(App.ServiceProvider.GetRequiredService<MainWindow>());
        }
        else
        {
            await MessageBoxManager.GetMessageBoxStandard("提示", string.Join("\n", item.OrderMaps.Select(x => x.OrderCode)!), ButtonEnum.Ok).ShowWindowDialogAsync(App.ServiceProvider.GetRequiredService<MainWindow>());
        }
    }

    [RelayCommand]
    private async Task OpenUrlAsync(int id)
    {
        var item = await _dataStore.GetProductAsync(id);
        if (item == null)
        {
            _notificationManager.Show(new Notification("错误", "数据不存在", NotificationType.Error));
            return;
        }

        var url = new Uri($"https://item.szlcsc.com/{item.ProductId}.html");

        await App.ServiceProvider.GetRequiredService<TopLevel>().Launcher.LaunchUriAsync(url);
    }

    [RelayCommand]
    private async Task BackupDbAsync()
    {
        IsBusy = true;

        try
        {
            await _dataStore.BackupAsync();

            _notificationManager.Show(new Notification("提示", "备份成功！", NotificationType.Success));
        }
        catch (System.Exception ex)
        {
            _notificationManager.Show(new Notification("错误", ex.Message, NotificationType.Error));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ShowOrderListAsync(int? fromProductId = 0)
    {
        var vm = App.ServiceProvider.GetRequiredService<OrderListViewModel>();

        await vm.InitialAsync(new Dictionary<string, object?> { { "fromProductId", fromProductId } });

        await DialogHost.ShowDialogAsync(new OrderListView(), vm, new DialogOptions()
        {
            Title = "订单列表",
            CanResize = false,
        });

        await LoadList();
    }

}
