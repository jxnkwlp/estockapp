using CommunityToolkit.Mvvm.ComponentModel;
using EStockApp.Models;
using EStockApp.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace EStockApp.ViewModels;

public partial class BrandDetailViewModel : DialogViewModelBase
{
    [ObservableProperty]
    private string? _brandName;

    [ObservableProperty]
    private ObservableCollection<BrandOrderLine> _orders = new();

    private readonly IDataStore _dataStore;

    public BrandDetailViewModel(IDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public override async Task InitialAsync(Dictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
    {
        await base.InitialAsync(properties, cancellationToken);

        Orders.Clear();

        if (properties == null || !properties.TryGetValue("brandName", out var brandObj) || brandObj is not string brandName
            || string.IsNullOrWhiteSpace(brandName))
            return;

        BrandName = brandName;

        foreach (var line in await _dataStore.GetOrderMapsByBrandAsync(brandName))
            Orders.Add(line);
    }
}
