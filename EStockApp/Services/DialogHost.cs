using Avalonia.Controls;
using EStockApp.ViewModels;
using EStockApp.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace EStockApp.Services;

public class DialogOptions
{
    public double? Width { get; set; }
    public double? Height { get; set; }
    public string? Title { get; set; }
    public bool CanResize { get; set; } = true;
}

public static class DialogHost
{
    public static void Show<TView, TViewModel>(TView view, TViewModel viewModel, DialogOptions? dialogOptions = null)
        where TView : UserControl
        where TViewModel : DialogViewModelBase
    {
        var window = CreateWindow(view, viewModel, dialogOptions);
        viewModel.OnClose = () => window.Close();
        window.Show();
    }

    public static async Task ShowDialogAsync<TView, TViewModel>(TView view, TViewModel viewModel, DialogOptions? dialogOptions = null)
       where TView : UserControl
       where TViewModel : DialogViewModelBase
    {
        var window = CreateWindow(view, viewModel, dialogOptions);
        viewModel.OnClose = () => window.Close();
        await window.ShowDialog(App.ServiceProvider.GetRequiredService<MainWindow>());
    }

    private static DialogHostWindow CreateWindow<TView, TViewModel>(TView view, TViewModel viewModel, DialogOptions? dialogOptions)
        where TView : UserControl
        where TViewModel : DialogViewModelBase
    {
        var width = dialogOptions?.Width ?? (double.IsNaN(view.Width) ? 800 : view.Width);
        var height = dialogOptions?.Height ?? (double.IsNaN(view.Height) ? 450 : view.Height);

        // Clear fixed size so the view stretches when the window is resized.
        view.Width = double.NaN;
        view.Height = double.NaN;

        var window = new DialogHostWindow(view, viewModel)
        {
            Width = width,
            Height = height,
            MinWidth = width,
            MinHeight = height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = dialogOptions?.CanResize ?? true,
        };

        if (dialogOptions?.Title != null)
            window.Title = dialogOptions.Title;

        return window;
    }
}
