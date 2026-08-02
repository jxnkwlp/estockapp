using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using EStockApp.ViewModels;

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
        if (AssociatedObject?.DataContext is not MainWindowViewModel vm)
            return;

        if (e.Key < Key.A || e.Key > Key.Z)
            return;

        var letter = (char)('A' + (e.Key - Key.A));
        if (!vm.TrySelectCategoryByLetter(letter))
            return;

        if (vm.SelectCategory is { } selected)
            AssociatedObject.ScrollIntoView(selected);

        e.Handled = true;
    }
}
