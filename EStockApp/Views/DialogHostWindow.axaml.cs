using Avalonia.Controls;
using EStockApp.ViewModels;

namespace EStockApp;

public partial class DialogHostWindow : Window
{
    public DialogHostWindow(UserControl view, ViewModelBase vm)
    {
        InitializeComponent();
        DataContext = this;
        View = view;
        View.DataContext = vm;
        Content.Content = View;
    }

    public UserControl View { get; }
}