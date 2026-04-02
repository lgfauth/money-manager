namespace MoneyManager.Infrastructure.Data.Migrations;

public interface IMigration
{
    /// <summary>
    /// Unique identifier for the migration. Use format "YYYYMMDD_NN_Description".
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Short description of what the migration does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Executes the migration against the database.
    /// </summary>
    Task ExecuteAsync(MongoDB.Driver.IMongoDatabase database);
}
