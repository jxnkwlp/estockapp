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
        var window = new DialogHostWindow(view, viewModel)
        {
            Width = view.Width,
            Height = view.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        viewModel.OnClose = () => window.Close();

        if (dialogOptions != null)
        {
            if (dialogOptions.Title != null) window.Title = dialogOptions.Title;
            if (dialogOptions.Width.HasValue) window.Width = dialogOptions.Width.Value;
            if (dialogOptions.Height.HasValue) window.Height = dialogOptions.Height.Value;
            window.CanResize = dialogOptions.CanResize;
        }

        window.Show();
    }

    public static async Task ShowDialogAsync<TView, TViewModel>(TView view, TViewModel viewModel, DialogOptions? dialogOptions = null)
       where TView : UserControl
       where TViewModel : DialogViewModelBase
    {
        var window = new DialogHostWindow(view, viewModel)
        {
            Width = view.Width,
            Height = view.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        viewModel.OnClose = () => window.Close();

        if (dialogOptions != null)
        {
            if (dialogOptions.Title != null) window.Title = dialogOptions.Title;
            if (dialogOptions.Width.HasValue) window.Width = dialogOptions.Width.Value;
            if (dialogOptions.Height.HasValue) window.Height = dialogOptions.Height.Value;
            window.CanResize = dialogOptions.CanResize;
        }

        await window.ShowDialog(App.ServiceProvider.GetRequiredService<MainWindow>());
    }
}
