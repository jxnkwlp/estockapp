using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace EStockApp.Controls;

public class DataGridTextSelectableColumn : DataGridTextColumn
{
    protected override Control GenerateElement(DataGridCell cell, object dataItem)
    {
        var root = new Border()
        {
            Name = "CellTextBlockBorder",
            Padding = new Avalonia.Thickness(10, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Background = Avalonia.Media.Brushes.Transparent,
        };

        var textBlock = new SelectableTextBlock
        {
            Name = "CellTextBlock",
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        };

        root.Child = textBlock;
        root.AddHandler(InputElement.PointerPressedEvent, OnCellPointerPressed, RoutingStrategies.Tunnel);

        if (Binding != null)
        {
            root.Child.Bind(SelectableTextBlock.TextProperty, Binding);

            var toolTipText = new TextBlock
            {
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            };

            toolTipText.Bind(TextBlock.TextProperty, Binding);

            ToolTip.SetTip(root, toolTipText);
        }

        if (CellTheme is { } theme)
        {
            root.Theme = theme;
        }

        return root;
    }

    private static void OnCellPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(sender as Control).Properties.IsLeftButtonPressed)
            return;

        if (sender is not Control control)
            return;

        var row = control.FindAncestorOfType<DataGridRow>();
        var grid = control.FindAncestorOfType<DataGrid>();
        if (row?.DataContext is null || grid is null)
            return;

        if (!Equals(grid.SelectedItem, row.DataContext))
            grid.SelectedItem = row.DataContext;
    }
}
