namespace EStockApp.Services.RemoteApi;

public class RemoteResult
{
    public RemoteResult()
    {
        Success = true;
    }

    public RemoteResult(string error)
    {
        Success = false;
        Error = error;
    }

    public bool Success { get; }
    public string? Error { get; }
}

public class RemoteResult<TResult> : RemoteResult
{
    public RemoteResult()
    {
    }

    public RemoteResult(string error) : base(error)
    {
    }

    public RemoteResult(TResult result) : base()
    {
        Result = result;
    }

    public TResult? Result { get; }
}
