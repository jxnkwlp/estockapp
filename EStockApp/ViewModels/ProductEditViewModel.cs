using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EStockApp.Data;
using EStockApp.Services;
using Nelibur.ObjectMapper;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EStockApp.ViewModels;

public partial class ProductEditViewModel : DialogViewModelBase
{
    private int? _id;

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
    }

    public override async Task InitialAsync(Dictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
    {
        await LoadCategory();
    }

    public async Task<bool> LoadAsync(int id)
    {
        _id = id;
        var item = await _dataStore.GetAsync(id);

        if (item == null)
        {
            _notificationManager.Show(new Notification("错误", "数据不存在", NotificationType.Error));
            return false;
        }

        EditItem = TinyMapper.Map<ItemEditViewModel>(item);

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

        if (_id.HasValue)
        {
            // 
            var newItem = TinyMapper.Map<Product>(EditItem);
            newItem.Id = _id.Value;
            await _dataStore.UpdateAsync(newItem);
        }
        else
        {
            await _dataStore.InsertAsync(TinyMapper.Map<Product>(EditItem));
        }

        _notificationManager.Show(new Notification("提示", "保存成功", NotificationType.Success));

        Close();
    }
}
