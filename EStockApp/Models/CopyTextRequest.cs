namespace EStockApp.Models;

public sealed class CopyTextRequest
{
    public required string Text { get; init; }
    public required string Label { get; init; }
}
