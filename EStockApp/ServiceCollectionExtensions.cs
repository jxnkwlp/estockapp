using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using EStockApp.Data;
using EStockApp.Models;
using EStockApp.Services;
using EStockApp.Services.OrderSync;
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
        services.AddTransient<OrderListView>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<SyncWindowViewModel>();

        services.AddTransient<ProductEditViewModel>();
        services.AddTransient<StockEditViewModel>();
        services.AddTransient<OrderListViewModel>();

        services.AddSingleton<IOrderHistorySync, BrowserOrderHistorySync>();

        services.AddSingleton<IDataStore>((s) =>
        {
            var instance = new LocalDataStore();
            instance.Initial();
            return instance;
        });

        services.AddSingleton<WindowNotificationManager>((s) => new WindowNotificationManager(TopLevel.GetTopLevel(s.GetRequiredService<MainWindow>())) { MaxItems = 3 });
        services.AddSingleton<TopLevel>((s) => TopLevel.GetTopLevel(s.GetRequiredService<MainWindow>())!);

        TinyMapper.Bind<Product, ProductItemModel>();
        TinyMapper.Bind<Product, ItemEditViewModel>();
        TinyMapper.Bind<ItemEditViewModel, Product>();
    }
}