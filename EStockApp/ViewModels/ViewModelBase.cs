using CommunityToolkit.Mvvm.ComponentModel;

namespace EStockApp.ViewModels;

public partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    public virtual void Initial() { }
}
