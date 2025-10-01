namespace EStockApp.Services.OrderSync;

public class SyncResult
{
    public SyncResult()
    {
        Success = true;
    }

    public SyncResult(string error)
    {
        Success = false;
        Error = error;
    }

    public bool Success { get; }
    public string? Error { get; }
}

public class SyncResult<TResult> : SyncResult
{
    public SyncResult()
    {
    }

    public SyncResult(string error) : base(error)
    {
    }

    public SyncResult(TResult result) : base()
    {
        Result = result;
    }

    public TResult? Result { get; }
}
