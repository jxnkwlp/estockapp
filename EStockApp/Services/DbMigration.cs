using System.Threading.Tasks;

namespace EStockApp.Services;

public class DbMigration
{
    public const string InitProductUsedCountId = "20251110_init_product_used_count";
    public const string FixOrderMapTotalPriceId = "20260808_fix_order_map_total_price";

    private const string LegacyUsedCountSettingKey = "migration_251110";

    private readonly IDataStore _dataStore;

    public DbMigration(IDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public async Task RunPendingAsync()
    {
        await RunInitProductUsedCountAsync();
        await RunFixOrderMapTotalPriceAsync();
    }

    private async Task RunInitProductUsedCountAsync()
    {
        if (await _dataStore.IsMigrationAppliedAsync(InitProductUsedCountId))
            return;

        var legacyApplied = !string.IsNullOrWhiteSpace(
            await _dataStore.GetSettingValueAsync(LegacyUsedCountSettingKey));

        if (!legacyApplied)
            await _dataStore.InitProductUsedCountAsync();

        await _dataStore.MarkMigrationAppliedAsync(InitProductUsedCountId);
    }

    private async Task RunFixOrderMapTotalPriceAsync()
    {
        if (await _dataStore.IsMigrationAppliedAsync(FixOrderMapTotalPriceId))
            return;

        await _dataStore.MigrateOrderMapTotalPricesAsync();
        await _dataStore.MarkMigrationAppliedAsync(FixOrderMapTotalPriceId);
    }
}
