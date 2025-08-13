using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EStockApp.Data;
using EStockApp.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace EStockApp.ViewModels;

public partial class OrderListViewModel : DialogViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<Order> _orders = new ObservableCollection<Order>();

    [ObservableProperty]
    private string? _filter;

    private readonly IDataStore _dataStore;

    public OrderListViewModel(IDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public override async Task InitialAsync(Dictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
    {
        await base.InitialAsync(properties, cancellationToken);

        await LoadListAsync();
    }

    private async Task LoadListAsync()
    {
        Orders.Clear();

        var list = await _dataStore.GetOrderListAsync();

        foreach (var item in list)
        {
            Orders.Add(item);
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await LoadListAsync();
    }
}
