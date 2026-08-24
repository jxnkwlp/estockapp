using System;

namespace EStockApp.Data;

public class MigrationRecord
{
    public string Id { get; set; } = null!;

    public DateTime AppliedAt { get; set; }
}
