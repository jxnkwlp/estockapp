using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using EStockApp.ViewModels;
using EStockApp.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace EStockApp;

public class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        foreach (var plugin in BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray())
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }

        ServiceCollection collection = new ServiceCollection();
        collection.AddCommonServices();

        var services = collection.BuildServiceProvider();

        ServiceProvider = services;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = services.GetRequiredService<MainWindow>();
            var vm = services.GetRequiredService<MainWindowViewModel>();
            window.DataContext = vm;
            vm.InitialAsync().ConfigureAwait(false);

            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}