using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EStockApp.Services;
using System.Threading.Tasks;

namespace EStockApp.ViewModels;

public partial class StockEditViewModel : DialogViewModelBase
{
    [ObservableProperty]
    private bool _isSet;

    [ObservableProperty]
    private int _value;

    private int _id;

    private readonly IDataStore _dataStore;

    public StockEditViewModel(IDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public void SetId(int id)
    {
        _id = id;
    }

    [RelayCommand]
    private async Task Save()
    {
        if (Value == 0 && !IsSet)
            return;

        if (IsSet && Value < 0)
        {
            return;
        }

        if (IsSet)
        {
            // 设置
            await _dataStore.SetStockAsync(_id, Value);
        }
        else
        {
            // 增减
            await _dataStore.IncreaseStockAsync(_id, Value);
        }

        Close();
    }
}
