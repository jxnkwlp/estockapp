using Avalonia.Controls;
using System;

namespace EStockApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // Force close
        Environment.Exit(0);
    }
}