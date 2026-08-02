using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;

namespace EStockApp.Behaviors;

public sealed class FocusControlOnKeyBehavior : Behavior<Control>
{
    public static readonly StyledProperty<Key> KeyProperty =
        AvaloniaProperty.Register<FocusControlOnKeyBehavior, Key>(nameof(Key), Key.F);

    public static readonly StyledProperty<KeyModifiers> KeyModifiersProperty =
        AvaloniaProperty.Register<FocusControlOnKeyBehavior, KeyModifiers>(nameof(KeyModifiers), KeyModifiers.Control);

    public static readonly StyledProperty<string?> TargetNameProperty =
        AvaloniaProperty.Register<FocusControlOnKeyBehavior, string?>(nameof(TargetName));

    public Key Key
    {
        get => GetValue(KeyProperty);
        set => SetValue(KeyProperty, value);
    }

    public KeyModifiers KeyModifiers
    {
        get => GetValue(KeyModifiersProperty);
        set => SetValue(KeyModifiersProperty, value);
    }

    public string? TargetName
    {
        get => GetValue(TargetNameProperty);
        set => SetValue(TargetNameProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject != null)
            AssociatedObject.KeyDown += OnKeyDown;
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject != null)
            AssociatedObject.KeyDown -= OnKeyDown;
        base.OnDetaching();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key || !e.KeyModifiers.HasFlag(KeyModifiers))
            return;

        if (string.IsNullOrEmpty(TargetName) || AssociatedObject is null)
            return;

        var target = AssociatedObject.GetVisualDescendants()
            .OfType<Control>()
            .FirstOrDefault(c => c.Name == TargetName);

        if (target is null)
            return;

        target.Focus();
        if (target is TextBox textBox)
            textBox.SelectAll();

        e.Handled = true;
    }
}
