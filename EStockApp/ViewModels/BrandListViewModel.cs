using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EStockApp.Models;
using EStockApp.Services;
using EStockApp.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EStockApp.ViewModels;

public partial class BrandListViewModel : DialogViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<BrandListItem> _brands = new();

    [ObservableProperty]
    private string? _filter;

    private readonly IDataStore _dataStore;
    private List<BrandListItem> _allBrands = [];

    public BrandListViewModel(IDataStore dataStore)
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
        _allBrands = await _dataStore.GetBrandListItemsAsync();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Brands.Clear();

        IEnumerable<BrandListItem> list = _allBrands;
        if (!string.IsNullOrWhiteSpace(Filter))
            list = list.Where(x => x.Name.Contains(Filter, System.StringComparison.OrdinalIgnoreCase));

        foreach (var item in list)
            Brands.Add(item);
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await LoadListAsync();
    }

    [RelayCommand]
    private async Task ShowDetailAsync(BrandListItem? item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.Name))
            return;

        var vm = App.ServiceProvider.GetRequiredService<BrandDetailViewModel>();
        await vm.InitialAsync(new Dictionary<string, object?> { { "brandName", item.Name } });

        await DialogHost.ShowDialogAsync(new BrandDetailView(), vm, new DialogOptions()
        {
            Title = $"品牌：{item.Name}",
        });
    }
}
