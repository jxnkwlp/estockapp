using System;

namespace EStockApp.Models;

public class SyncRecordLine
{
    public string ActionText { get; set; } = null!;
    public string OrderNo { get; set; } = null!;
    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public int Quantity { get; set; }
    public DateTime SyncedAt { get; set; }
}
