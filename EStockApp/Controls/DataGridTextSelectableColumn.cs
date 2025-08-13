using Avalonia.Controls;

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
        };

        var textBlock = new SelectableTextBlock
        {
            Name = "CellTextBlock",
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        };

        root.Child = textBlock;

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
}
