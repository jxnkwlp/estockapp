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
using System;
using System.Collections.ObjectModel;
using System.Linq;
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
    private int _totalCount;
    [ObservableProperty]
    private int _totalStockCount;

    private readonly IDataStore _dataStore;
    private readonly WindowNotificationManager _notificationManager;

    public MainWindowViewModel(IDataStore dataStore, WindowNotificationManager notificationManager)
    {
        _dataStore = dataStore;
        _notificationManager = notificationManager;
    }

    public override void Initial()
    {
        _ = LoadCategory();
        _ = LoadList();
    }

    [RelayCommand]
    private async Task ShowSyncViewAsync()
    {
        var historySyncWindow = App.ServiceProvider.GetRequiredService<SyncWindow>();
        historySyncWindow.DataContext = App.ServiceProvider.GetRequiredService<SyncWindowViewModel>();
        await historySyncWindow.ShowDialog(App.ServiceProvider.GetRequiredService<MainWindow>());
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
            TotalCount = await _dataStore.GetCountAsync();
            TotalStockCount = await _dataStore.GetStockCountAsync();

            var list = await _dataStore.GetListAsync(int.MaxValue, 0, SelectCategory == "全部" ? null : SelectCategory, ListFilter);
            Items.Clear();
            Items = new ObservableCollection<ProductItemModel>(list);
        }
        catch (System.Exception ex)
        {
            _notificationManager.Show(new Notification("错误", ex.Message, NotificationType.Error));
        }
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

        vm.Initial();
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

        vm.Initial();

        await DialogHost.ShowDialogAsync(new ProductEditView(), vm, new DialogOptions()
        {
            Title = "编辑",
            CanResize = false,
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

        vm.Initial();

        await DialogHost.ShowDialogAsync(new ProductEditView(), vm, new DialogOptions()
        {
            Title = "编辑",
            CanResize = false,
        });

        await LoadList();
    }

    [RelayCommand]
    private async Task DeleteAsync(int id)
    {
        var item = await _dataStore.GetAsync(id);
        if (item == null)
        {
            _notificationManager.Show(new Notification("错误", "数据不存在", NotificationType.Error));
            return;
        }

        var box = MessageBoxManager.GetMessageBoxStandard("删除确认", $"{item.BrandName}\n{item.Category}\n{item.ProductName}\n{item.Pack}\n{item.ProductCode}", ButtonEnum.YesNo);

        if (await box.ShowWindowDialogAsync(App.ServiceProvider.GetRequiredService<MainWindow>()) == ButtonResult.Yes)
        {
            await _dataStore.DeleteAsync(id);
        }

        await LoadList();
    }

    [RelayCommand]
    private async Task ShowOrderNoAsync(int id)
    {
        var item = await _dataStore.GetAsync(id);
        if (item == null)
        {
            _notificationManager.Show(new Notification("错误", "数据不存在", NotificationType.Error));
            return;
        }

        if (item.OrderCodes?.Any() != true)
        {
            await MessageBoxManager.GetMessageBoxStandard("提示", "无相关订单", ButtonEnum.Ok).ShowWindowDialogAsync(App.ServiceProvider.GetRequiredService<MainWindow>());
        }
        else
        {
            await MessageBoxManager.GetMessageBoxStandard("提示", string.Join("\n", item.OrderCodes!), ButtonEnum.Ok).ShowWindowDialogAsync(App.ServiceProvider.GetRequiredService<MainWindow>());
        }
    }

    [RelayCommand]
    private async Task OpenUrlAsync(int id)
    {
        var item = await _dataStore.GetAsync(id);
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
}
