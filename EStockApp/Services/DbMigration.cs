using System.Threading.Tasks;

namespace EStockApp.Services;

public class DbMigration
{
    private readonly IDataStore _dataStore;

    public DbMigration(IDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public async Task MigrateAsync()
    {
        await _dataStore.InitProductUsedCountAsync();
    }
}
