using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EStockApp.ViewModels;

public partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    public virtual Task InitialAsync(Dictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
