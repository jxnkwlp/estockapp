namespace EStockApp.Services.OrderSync;

public class SyncResult
{
    public SyncResult(SyncStatus status, string? message = null)
    {
        Status = status;
        Message = message;
    }

    public SyncStatus Status { get; } = SyncStatus.Success;
    public string? Message { get; }
}

public class SyncResult<TResult> : SyncResult
{
    public SyncResult(TResult result, string? message = null) : base(SyncStatus.Success, message)
    {
        Result = result;
    }

    public SyncResult(SyncStatus status, string? message = null) : base(status, message)
    {
    }

    public TResult? Result { get; }
}
