using System.Windows.Input;

namespace EStockApp.Models;

public sealed class ContextMenuActionItem
{
    public bool IsSeparator { get; init; }
    public string? Header { get; init; }
    public ICommand? Command { get; init; }
    public object? CommandParameter { get; init; }
}
