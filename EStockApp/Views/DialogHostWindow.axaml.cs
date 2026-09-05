using Avalonia.Controls;
using EStockApp.ViewModels;

namespace EStockApp;

public partial class DialogHostWindow : Window
{
    public DialogHostWindow()
    {
        InitializeComponent();
    }

    public DialogHostWindow(UserControl view, ViewModelBase vm)
        : this()
    {
        DataContext = this;
        View = view;
        View.DataContext = vm;
        Host.Content = View;
    }

    public UserControl? View { get; }
}
