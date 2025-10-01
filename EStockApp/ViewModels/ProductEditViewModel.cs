using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EStockApp.Data;
using EStockApp.Services;
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
    private readonly WindowNotificationManager _notificationManager;

    public ProductEditViewModel(IDataStore dataStore, WindowNotificationManager notificationManager)
    {
        _dataStore = dataStore;
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
        _product = await _dataStore.GetAsync(id);

        if (_product == null)
        {
            _notificationManager.Show(new Notification("错误", "数据不存在", NotificationType.Error));
            return false;
        }

        EditItem = TinyMapper.Map<ItemEditViewModel>(_product);

        EditItem.IsAdd = false;

        return true;
    }

    private async Task LoadCategory()
    {
        var categoryList = (await _dataStore.GetCategoryListAsync()).ToList();

        CategoryList.Clear();
        CategoryList = new ObservableCollection<string>(categoryList);
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
                await _dataStore.UpdateAsync(
                    productId: EditItem.ProductId,
                    category: EditItem.Category!,
                    productCode: EditItem.ProductCode!,
                    productName: EditItem.ProductName!,
                    productModel: EditItem.ProductModel,
                    brandName: EditItem.BrandName,
                    pack: EditItem.Pack,
                    stockUnitName: EditItem.StockUnitName,
                    unitPrice: EditItem.UnitPrice);

                if (_product!.TotalCount != EditItem.TotalCount)
                {
                    await _dataStore.UpdateTotalCountAsync(_product!.ProductId, EditItem.TotalCount);
                }
            }
            else
            {
                if (await _dataStore.IsExistsAsync(EditItem.ProductId))
                {
                    throw new System.Exception($"产品ID = {EditItem.ProductId} 已存在");
                }

                await _dataStore.InsertAsync(
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
                    await _dataStore.AddOrderMapAsync(EditItem.ProductId, EditItem.OrderNo, EditItem.UnitPrice, EditItem.TotalCount, EditItem.TotalPrice);
                    await _dataStore.InsertOrderAsync(EditItem.OrderNo, EditItem.OrderNo, EditItem.TotalPrice, 0, EditItem.TotalPrice, DateTime.Now, EditItem.TotalCount);
                }
                else
                    await _dataStore.UpdateTotalCountAsync(_product!.ProductId, EditItem.TotalCount);
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
