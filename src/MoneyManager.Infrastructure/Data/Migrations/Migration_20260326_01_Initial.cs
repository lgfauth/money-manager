using MongoDB.Driver;

namespace MoneyManager.Infrastructure.Data.Migrations;

public class Migration_20260326_01_Initial : IMigration
{
    public string Id => "20260326_01_Initial";
    public string Description => "Initial migration marker — baseline schema";

    public Task ExecuteAsync(IMongoDatabase database)
    {
        // Baseline migration: no schema changes needed.
        // Indexes are managed by MongoContext.CreateCollectionsAndIndexesAsync().
        // This migration exists as the starting point for the migration history.
        return Task.CompletedTask;
    }
}
