using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using EStockApp.Helpers;

namespace EStockApp.Behaviors;

public sealed class CategoryLetterNavigationBehavior : Behavior<ComboBox>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject?.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    protected override void OnDetaching()
    {
        AssociatedObject?.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
        base.OnDetaching();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (AssociatedObject == null)
            return;

        if (e.Key < Key.A || e.Key > Key.Z)
            return;

        var letter = (char)('A' + (e.Key - Key.A));
        var categories = AssociatedObject.Items
            .OfType<string>()
            .ToList();

        if (categories.Count == 0 && AssociatedObject.ItemsSource is IEnumerable<string> source)
            categories = source.ToList();

        if (!CategorySelectionHelper.TrySelectNext(categories, AssociatedObject.SelectedItem as string, letter, out var selected)
            || selected == null)
            return;

        AssociatedObject.SelectedItem = selected;
        AssociatedObject.ScrollIntoView(selected);
        e.Handled = true;
    }
}
