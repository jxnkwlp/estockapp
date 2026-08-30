using CommunityToolkit.Mvvm.ComponentModel;
using EStockApp.Models;
using EStockApp.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace EStockApp.ViewModels;

public partial class OrderDetailViewModel : DialogViewModelBase
{
    [ObservableProperty]
    private string? _orderNo;

    [ObservableProperty]
    private ObservableCollection<OrderProductLine> _items = new();

    private readonly IDataStore _dataStore;

    public OrderDetailViewModel(IDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public override async Task InitialAsync(Dictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
    {
        await base.InitialAsync(properties, cancellationToken);

        Items.Clear();

        if (properties == null || !properties.TryGetValue("orderNo", out var orderObj) || orderObj is not string orderNo
            || string.IsNullOrWhiteSpace(orderNo))
            return;

        OrderNo = orderNo;

        foreach (var line in await _dataStore.GetProductsByOrderNoAsync(orderNo))
            Items.Add(line);
    }
}
