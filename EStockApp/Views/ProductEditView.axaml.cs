using Avalonia;
using Avalonia.Controls;
using EStockApp.ViewModels;

namespace EStockApp;

public partial class ProductEditView : UserControl
{
    public ProductEditView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (DataContext is ProductEditViewModel vm)
        {
            vm.SyncCategorySelection();
        }
    }
}
