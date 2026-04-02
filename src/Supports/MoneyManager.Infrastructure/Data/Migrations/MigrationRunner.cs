using MongoDB.Driver;

namespace MoneyManager.Infrastructure.Data.Migrations;

public class MigrationRunner
{
    private const string MigrationsCollectionName = "_migrations";
    private readonly IMongoDatabase _database;

    public MigrationRunner(IMongoDatabase database)
    {
        _database = database;
    }

    public async Task RunAsync(IEnumerable<IMigration> migrations)
    {
        var collection = _database.GetCollection<MigrationRecord>(MigrationsCollectionName);

        var appliedIds = await collection
            .Find(FilterDefinition<MigrationRecord>.Empty)
            .Project(r => r.Id)
            .ToListAsync();

        var appliedSet = new HashSet<string>(appliedIds);

        foreach (var migration in migrations.OrderBy(m => m.Id))
        {
            if (appliedSet.Contains(migration.Id))
                continue;

            await migration.ExecuteAsync(_database);

            await collection.InsertOneAsync(new MigrationRecord
            {
                Id = migration.Id,
                Description = migration.Description,
                AppliedAt = DateTime.UtcNow
            });
        }
    }
}
