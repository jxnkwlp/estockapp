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
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EStockApp.Helpers;
using Notification = Avalonia.Controls.Notifications.Notification;
using WindowNotificationManager = Avalonia.Controls.Notifications.WindowNotificationManager;

namespace EStockApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
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

    [ObservableProperty]
    private string? _sortMemberPath;

    [ObservableProperty]
    private ListSortDirection _sortDirection = ListSortDirection.Ascending;

    private readonly IDataStore _dataStore;
    private readonly WindowNotificationManager _notificationManager;
    private readonly DbMigration _dbMigration;

    private bool _isReady;
    private bool _suppressCategoryAutoSearch;
    private int _searchVersion;

    public MainWindowViewModel(IDataStore dataStore, WindowNotificationManager notificationManager, DbMigration dbMigration)
    {
        _dataStore = dataStore;
        _notificationManager = notificationManager;
        _dbMigration = dbMigration;
    }

    public override async Task InitialAsync(Dictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
    {
        await _dataStore.RebuildAsync();

        _suppressCategoryAutoSearch = true;
        await LoadSummaryAsync();
        await LoadCategory();
        await LoadList();
        _suppressCategoryAutoSearch = false;
        _isReady = true;

        var migration_251110 = await _dataStore.GetSettingValueAsync("migration_251110");
        if (string.IsNullOrWhiteSpace(migration_251110))
        {
            await _dbMigration.MigrateAsync();

            await _dataStore.SetSettingValueAsync("migration_251110", "1");
        }
    }

    private string? ActiveCategoryFilter => SelectCategory == "全部" || string.IsNullOrEmpty(SelectCategory) ? null : SelectCategory;

    partial void OnSelectCategoryChanged(string? value)
    {
        if (!_isReady || _suppressCategoryAutoSearch)
            return;

        _ = SearchAsync();
    }

    public void SetSort(string? memberPath, ListSortDirection direction)
    {
        SortMemberPath = string.IsNullOrWhiteSpace(memberPath) ? null : memberPath;
        SortDirection = direction;
    }

    public void ClearSort()
    {
        SortMemberPath = null;
        SortDirection = ListSortDirection.Ascending;
    }

    public bool TrySelectCategoryByLetter(char letter)
    {
        if (!CategorySelectionHelper.TrySelectNext(CategoryList, SelectCategory, letter, out var selected)
            || selected == null)
            return false;

        SelectCategory = selected;
        return true;
    }

    public IReadOnlyList<ContextMenuActionItem> BuildRowContextActions(ProductItemModel item, string? columnTag)
    {
        var actions = new List<ContextMenuActionItem>();
        columnTag ??= string.Empty;
        var cellText = GetCellText(item, columnTag);
        var cellLabel = GetColumnLabel(columnTag);
        var hasPrimaryAction = false;

        if (columnTag == "Category" && !string.IsNullOrWhiteSpace(item.Category))
        {
            actions.Add(new ContextMenuActionItem
            {
                Header = $"选择 {item.Category}",
                Command = SelectCategoryFromCellCommand,
                CommandParameter = item.Category,
            });
            hasPrimaryAction = true;
        }
        else if (IsFilterableColumn(columnTag) && !string.IsNullOrWhiteSpace(cellText))
        {
            actions.Add(new ContextMenuActionItem
            {
                Header = $"过滤 {cellText}",
                Command = FilterByValueCommand,
                CommandParameter = cellText,
            });
            hasPrimaryAction = true;
        }

        if (!string.IsNullOrEmpty(cellText) && columnTag != "Actions")
        {
            if (hasPrimaryAction)
                actions.Add(new ContextMenuActionItem { IsSeparator = true });

            actions.Add(new ContextMenuActionItem
            {
                Header = string.IsNullOrEmpty(cellLabel) ? "复制" : $"复制{cellLabel}",
                Command = CopyTextCommand,
                CommandParameter = new CopyTextRequest { Text = cellText, Label = cellLabel },
            });
        }

        var addModelCopy = !string.IsNullOrWhiteSpace(item.ProductModel) && columnTag != "ProductModel";
        var addCodeCopy = !string.IsNullOrWhiteSpace(item.ProductCode) && columnTag != "ProductCode";
        if (addModelCopy || addCodeCopy)
        {
            if (actions.Count > 0)
                actions.Add(new ContextMenuActionItem { IsSeparator = true });

            if (addModelCopy)
            {
                actions.Add(new ContextMenuActionItem
                {
                    Header = "复制型号",
                    Command = CopyTextCommand,
                    CommandParameter = new CopyTextRequest { Text = item.ProductModel!, Label = "型号" },
                });
            }

            if (addCodeCopy)
            {
                actions.Add(new ContextMenuActionItem
                {
                    Header = "复制编号",
                    Command = CopyTextCommand,
                    CommandParameter = new CopyTextRequest { Text = item.ProductCode, Label = "编号" },
                });
            }
        }

        if (actions.Count > 0)
            actions.Add(new ContextMenuActionItem { IsSeparator = true });

        actions.Add(new ContextMenuActionItem
        {
            Header = "清除筛选",
            Command = ClearFiltersCommand,
        });

        return actions;
    }

    private static bool IsFilterableColumn(string columnTag) =>
        columnTag is "ProductName" or "ProductModel" or "Pack" or "BrandName" or "ProductCode";

    private static string GetColumnLabel(string columnTag) => columnTag switch
    {
        "Category" => "类目",
        "ProductName" => "名称",
        "ProductModel" => "型号",
        "Pack" => "封装",
        "BrandName" => "品牌",
        "ProductCode" => "编号",
        "StockCount" => "数量",
        _ => string.Empty,
    };

    private static string? GetCellText(ProductItemModel item, string columnTag) => columnTag switch
    {
        "Category" => item.Category,
        "ProductName" => item.ProductName,
        "ProductModel" => item.ProductModel,
        "Pack" => item.Pack,
        "BrandName" => item.BrandName,
        "ProductCode" => item.ProductCode,
        "StockCount" => $"{item.StockCount}/{item.TotalCount}",
        _ => null,
    };

    [RelayCommand]
    private async Task ShowSyncViewAsync()
    {
        var historySyncWindow = App.ServiceProvider.GetRequiredService<SyncWindow>();
        historySyncWindow.DataContext = App.ServiceProvider.GetRequiredService<SyncWindowViewModel>();
        await historySyncWindow.ShowDialog(App.ServiceProvider.GetRequiredService<MainWindow>());

        _suppressCategoryAutoSearch = true;
        ListFilter = null;
        SelectCategory = "全部";
        _suppressCategoryAutoSearch = false;

        await LoadSummaryAsync();
        await LoadCategory();
        await LoadList();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        var version = Interlocked.Increment(ref _searchVersion);
        IsBusy = true;

        try
        {
            await LoadList();
            if (version != _searchVersion)
                return;

            await LoadSummaryAsync();
        }
        catch (System.Exception ex)
        {
            _notificationManager.Show(new Notification("错误", ex.Message, NotificationType.Error));
        }
        finally
        {
            if (version == _searchVersion)
                IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        _suppressCategoryAutoSearch = true;
        SelectCategory = "全部";
        ListFilter = null;
        _suppressCategoryAutoSearch = false;

        await SearchAsync();
    }

    [RelayCommand]
    private async Task SelectCategoryFromCellAsync(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return;

        if (!CategoryList.Contains(category))
        {
            _notificationManager.Show(new Notification("提示", $"类目不存在：{category}", NotificationType.Warning));
            return;
        }

        if (SelectCategory == category)
        {
            await SearchAsync();
            return;
        }

        SelectCategory = category;
    }

    [RelayCommand]
    private async Task FilterByValueAsync(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        ListFilter = value.Trim();
        await SearchAsync();
    }

    [RelayCommand]
    private async Task CopyTextAsync(CopyTextRequest? request)
    {
        if (request is null || string.IsNullOrEmpty(request.Text))
            return;

        var clipboard = App.ServiceProvider.GetRequiredService<TopLevel>().Clipboard;
        if (clipboard == null)
            return;

        await clipboard.SetTextAsync(request.Text);
        var message = string.IsNullOrWhiteSpace(request.Label) ? "已复制" : $"已复制{request.Label}";
        _notificationManager.Show(new Notification("提示", message, NotificationType.Success));
    }

    private async Task LoadList()
    {
        try
        {
            var list = await _dataStore.GetProductListAsync(int.MaxValue, 0, ActiveCategoryFilter, ListFilter);
            var mapped = list.Select(x => TinyMapper.Map<ProductItemModel>(x));
            Items = new ObservableCollection<ProductItemModel>(ApplySort(mapped));
        }
        catch (System.Exception ex)
        {
            _notificationManager.Show(new Notification("错误", ex.Message, NotificationType.Error));
        }
    }

    private IEnumerable<ProductItemModel> ApplySort(IEnumerable<ProductItemModel> source)
    {
        if (string.IsNullOrEmpty(SortMemberPath))
            return source;

        IOrderedEnumerable<ProductItemModel> ordered = SortMemberPath switch
        {
            nameof(ProductItemModel.Category) => OrderBy(source, x => x.Category),
            nameof(ProductItemModel.ProductName) => OrderBy(source, x => x.ProductName),
            nameof(ProductItemModel.ProductModel) => OrderBy(source, x => x.ProductModel),
            nameof(ProductItemModel.Pack) => OrderBy(source, x => x.Pack),
            nameof(ProductItemModel.BrandName) => OrderBy(source, x => x.BrandName),
            nameof(ProductItemModel.ProductCode) => OrderBy(source, x => x.ProductCode),
            nameof(ProductItemModel.StockCount) => OrderBy(source, x => x.StockCount),
            _ => source.OrderBy(_ => 0),
        };

        return ordered;
    }

    private IOrderedEnumerable<ProductItemModel> OrderBy<TKey>(IEnumerable<ProductItemModel> source, Func<ProductItemModel, TKey> keySelector) =>
        SortDirection == ListSortDirection.Ascending
            ? source.OrderBy(keySelector)
            : source.OrderByDescending(keySelector);

    private async Task LoadSummaryAsync()
    {
        TotalCategoryCount = await _dataStore.GetProductCountAsync(ActiveCategoryFilter, ListFilter);
        TotalCount = await _dataStore.GetTotalCountAsync(ActiveCategoryFilter, ListFilter);
        TotalStockCount = await _dataStore.GetStockCountAsync(ActiveCategoryFilter, ListFilter);
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
        await LoadSummaryAsync();
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
        await LoadSummaryAsync();
    }

    [RelayCommand]
    private async Task EditAsync(int id)
    {
        var vm = App.ServiceProvider.GetRequiredService<ProductEditViewModel>();

        if (!await vm.LoadAsync(id))
        {
            return;
        }

        await DialogHost.ShowDialogAsync(new ProductEditView(), vm, new DialogOptions()
        {
            Title = "编辑",
            CanResize = false,
            Height = 510,
        });

        await LoadList();
        await LoadSummaryAsync();
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
        await LoadSummaryAsync();
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
        await LoadSummaryAsync();
    }

}
