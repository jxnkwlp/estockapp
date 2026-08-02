using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EStockApp.Data;
using EStockApp.Services;
using EStockApp.Services.RemoteApi;
using Nelibur.ObjectMapper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EStockApp.ViewModels;

public partial class ProductEditViewModel : DialogViewModelBase
{
    private int? _id;

    private Product? _product;

    [ObservableProperty]
    private ItemEditViewModel _editItem = new ItemEditViewModel();

    [ObservableProperty]
    private ObservableCollection<string> _categoryList = new ObservableCollection<string>();

    private readonly IDataStore _dataStore;
    private readonly IRemoteApi _remoteApi;
    private readonly WindowNotificationManager _notificationManager;

    public ProductEditViewModel(IDataStore dataStore, IRemoteApi remoteApi, WindowNotificationManager notificationManager)
    {
        _dataStore = dataStore;
        _remoteApi = remoteApi;
        _notificationManager = notificationManager;

        EditItem.IsAdd = true;
    }

    public override async Task InitialAsync(Dictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
    {
        await LoadCategory();
    }

    public async Task<bool> LoadAsync(int id)
    {
        _id = id;
        _product = await _dataStore.GetProductAsync(id);

        if (_product == null)
        {
            _notificationManager.Show(new Notification("错误", "数据不存在", NotificationType.Error));
            return false;
        }

        EditItem = TinyMapper.Map<ItemEditViewModel>(_product);
        EditItem.IsAdd = false;

        await LoadCategory();

        return true;
    }

    private async Task LoadCategory()
    {
        var categoryList = (await _dataStore.GetCategoryListAsync()).ToList();
        CategoryList = new ObservableCollection<string>(categoryList);
        SyncCategorySelection();
    }

    /// <summary>
    /// Ensures the current category exists in <see cref="CategoryList"/> and refreshes ComboBox selection.
    /// </summary>
    public void SyncCategorySelection()
    {
        if (!string.IsNullOrWhiteSpace(EditItem.Category))
        {
            var current = EditItem.Category.Trim();
            var match = CategoryList.FirstOrDefault(c => string.Equals(c, current, StringComparison.Ordinal));
            if (match == null)
            {
                CategoryList.Add(current);
                match = current;
            }

            // Force ComboBox rebind: same value may not raise PropertyChanged.
            EditItem.Category = null;
            EditItem.Category = match;
            return;
        }

        if (EditItem.IsAdd && CategoryList.Count > 0)
        {
            EditItem.Category = CategoryList[0];
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task LoadFromRemoteAsync()
    {
        if (string.IsNullOrWhiteSpace(EditItem.ProductCode))
        {
            _notificationManager.Show(new Notification("提示", "请先填写编号", NotificationType.Warning));
            return;
        }

        IsBusy = true;

        try
        {
            var result = await _remoteApi.GetProductDetailAsync(EditItem.ProductCode.Trim());
            if (!result.Success || result.Result == null)
            {
                _notificationManager.Show(new Notification("错误", result.Error ?? "加载失败", NotificationType.Error));
                return;
            }

            var detail = result.Result;
            EditItem.ProductId = detail.ProductId;
            EditItem.ProductModel = detail.ProductModel;
            EditItem.ProductName = detail.ProductName;
            EditItem.Pack = detail.Pack;
            EditItem.BrandName = detail.BrandName;
            EditItem.ProductCode = detail.ProductCode;
            EditItem.StockUnitName = detail.StockUnitName;
            EditItem.UnitPrice = detail.UnitPrice;
            EditItem.Category = detail.Category;
            SyncCategorySelection();

            _notificationManager.Show(new Notification("提示", "基本信息已加载", NotificationType.Success));
        }
        catch (Exception ex)
        {
            _notificationManager.Show(new Notification("错误", ex.Message, NotificationType.Error));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task SaveAsync()
    {
        EditItem.Validate();

        if (EditItem.HasErrors)
        {
            return;
        }

        try
        {

            if (_product != null)
            {
                await _dataStore.UpdateProductAsync(
                    productId: EditItem.ProductId,
                    category: EditItem.Category!,
                    productCode: EditItem.ProductCode!,
                    productName: EditItem.ProductName!,
                    productModel: EditItem.ProductModel,
                    brandName: EditItem.BrandName,
                    pack: EditItem.Pack,
                    stockUnitName: EditItem.StockUnitName,
                    unitPrice: EditItem.UnitPrice);

                if (_product.TotalCount != EditItem.TotalCount)
                {
                    await _dataStore.UpdateProductTotalCountAsync(_product.ProductId, EditItem.TotalCount);
                }
            }
            else
            {
                if (await _dataStore.IsProductExistsAsync(EditItem.ProductId))
                {
                    throw new System.Exception($"产品ID = {EditItem.ProductId} 已存在");
                }

                var id = await _dataStore.InsertProductAsync(
                    productId: EditItem.ProductId,
                    category: EditItem.Category!,
                    productCode: EditItem.ProductCode!,
                    productName: EditItem.ProductName!,
                    productModel: EditItem.ProductModel,
                    brandName: EditItem.BrandName,
                    pack: EditItem.Pack,
                    stockUnitName: EditItem.StockUnitName,
                    unitPrice: EditItem.UnitPrice);

                if (!string.IsNullOrWhiteSpace(EditItem.OrderNo))
                {
                    await _dataStore.AddProductOrderMapAsync(EditItem.ProductId, EditItem.OrderNo, EditItem.UnitPrice, EditItem.TotalCount, EditItem.TotalPrice);
                    await _dataStore.InsertOrderAsync(EditItem.OrderNo, EditItem.OrderNo, EditItem.TotalPrice, 0, EditItem.TotalPrice, DateTime.Now, EditItem.TotalCount);
                }
                else
                {
                    await _dataStore.UpdateProductTotalCountAsync(EditItem.ProductId, EditItem.TotalCount);
                    await _dataStore.SetProductStockAsync(id, EditItem.StockCount);
                }
            }

            _notificationManager.Show(new Notification("提示", "保存成功", NotificationType.Success));
        }
        catch (System.Exception ex)
        {
            _notificationManager.Show(new Notification("错误", ex.Message, NotificationType.Error));

            return;
        }

        Close();
    }
}
