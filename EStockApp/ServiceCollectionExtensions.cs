using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using EStockApp.Models;
using EStockApp.Services;
using EStockApp.ViewModels;
using EStockApp.Views;
using Microsoft.Extensions.DependencyInjection;
using Nelibur.ObjectMapper;

namespace EStockApp;

public static class ServiceCollectionExtensions
{
    public static void AddCommonServices(this IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();
        services.AddTransient<SyncWindow>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<SyncWindowViewModel>();

        services.AddTransient<ProductEditViewModel>();
        services.AddTransient<StockEditViewModel>();

        services.AddSingleton<IDataStore, LocalDataStore>();

        services.AddSingleton<WindowNotificationManager>((s) => new WindowNotificationManager(TopLevel.GetTopLevel(s.GetRequiredService<MainWindow>())) { MaxItems = 3 });
        services.AddSingleton<TopLevel>((s) => TopLevel.GetTopLevel(s.GetRequiredService<MainWindow>())!);

        TinyMapper.Bind<ProductItemModel, ItemEditViewModel>();
        TinyMapper.Bind<ItemEditViewModel, ProductItemModel>();
    }
}