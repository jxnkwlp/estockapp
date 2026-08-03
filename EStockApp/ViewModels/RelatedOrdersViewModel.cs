using CommunityToolkit.Mvvm.ComponentModel;
using EStockApp.Data;
using EStockApp.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace EStockApp.ViewModels;

public partial class RelatedOrdersViewModel : DialogViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<ProductOrderMap> _orders = new ObservableCollection<ProductOrderMap>();

    private readonly IDataStore _dataStore;

    public RelatedOrdersViewModel(IDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public override async Task InitialAsync(Dictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
    {
        await base.InitialAsync(properties, cancellationToken);

        Orders.Clear();

        if (properties == null || !properties.TryGetValue("productId", out var productIdObj) || productIdObj is not int productId)
            return;

        var product = await _dataStore.GetProductAsync(productId);
        if (product?.OrderMaps == null)
            return;

        foreach (var map in product.OrderMaps)
        {
            var expectedTotalPrice = map.UnitPrice * map.TotalCount - map.Discount;
            if (map.TotalPrice != expectedTotalPrice)
            {
                map.TotalPrice = expectedTotalPrice;
                await _dataStore.AddProductOrderMapAsync(
                    product.ProductId,
                    map.OrderCode,
                    map.UnitPrice,
                    map.TotalCount,
                    map.TotalPrice,
                    map.Discount);
            }

            Orders.Add(map);
        }
    }
}
