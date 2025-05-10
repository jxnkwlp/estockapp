using System;

namespace EStockApp.ViewModels;

public class DialogViewModelBase : ViewModelBase
{
    public Action? OnClose { get; set; }

    public void Close()
    {
        OnClose?.Invoke();
    }
}
