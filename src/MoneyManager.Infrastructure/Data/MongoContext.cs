using MongoDB.Driver;
using MongoDB.Bson;
using System.Security.Authentication;

namespace MoneyManager.Infrastructure.Data;

public class MongoContext
{
    private readonly IMongoDatabase _database;
    private readonly MongoClient _client;

    public MongoContext(MongoSettings settings)
    {
        var client = new MongoClient(settings.ConnectionString);
        _client = client;
        _database = client.GetDatabase(settings.DatabaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string collectionName)
    {
        return _database.GetCollection<T>(collectionName);
    }

    public async Task CreateCollectionsAndIndexesAsync()
    {
        // Create collections if they don't exist
        var cursor = await _database.ListCollectionNamesAsync();
        var collectionNames = cursor.ToList();

        // Create users collection
        if (!collectionNames.Contains("users"))
        {
            await _database.CreateCollectionAsync("users");
        }
        var usersCollection = _database.GetCollection<MoneyManager.Domain.Entities.User>("users");
        await usersCollection.Indexes.CreateOneAsync(new CreateIndexModel<MoneyManager.Domain.Entities.User>(
            Builders<MoneyManager.Domain.Entities.User>.IndexKeys.Ascending(u => u.Email)
        ));

        // Create categories collection
        if (!collectionNames.Contains("categories"))
        {
            await _database.CreateCollectionAsync("categories");
        }
        var categoriesCollection = _database.GetCollection<MoneyManager.Domain.Entities.Category>("categories");
        await categoriesCollection.Indexes.CreateOneAsync(new CreateIndexModel<MoneyManager.Domain.Entities.Category>(
            Builders<MoneyManager.Domain.Entities.Category>.IndexKeys
                .Ascending(c => c.UserId)
                .Ascending(c => c.CreatedAt)
        ));

        // Create transactions collection
        if (!collectionNames.Contains("transactions"))
        {
            await _database.CreateCollectionAsync("transactions");
        }
        var transactionsCollection = _database.GetCollection<MoneyManager.Domain.Entities.Transaction>("transactions");
        await transactionsCollection.Indexes.CreateOneAsync(new CreateIndexModel<MoneyManager.Domain.Entities.Transaction>(
            Builders<MoneyManager.Domain.Entities.Transaction>.IndexKeys
                .Ascending(t => t.UserId)
                .Ascending(t => t.Date)
        ));

        // Create accounts collection
        if (!collectionNames.Contains("accounts"))
        {
            await _database.CreateCollectionAsync("accounts");
        }
        var accountsCollection = _database.GetCollection<MoneyManager.Domain.Entities.Account>("accounts");
        await accountsCollection.Indexes.CreateOneAsync(new CreateIndexModel<MoneyManager.Domain.Entities.Account>(
            Builders<MoneyManager.Domain.Entities.Account>.IndexKeys
                .Ascending(a => a.UserId)
        ));

        // Create budgets collection
        if (!collectionNames.Contains("budgets"))
        {
            await _database.CreateCollectionAsync("budgets");
        }
        var budgetsCollection = _database.GetCollection<MoneyManager.Domain.Entities.Budget>("budgets");
        await budgetsCollection.Indexes.CreateOneAsync(new CreateIndexModel<MoneyManager.Domain.Entities.Budget>(
            Builders<MoneyManager.Domain.Entities.Budget>.IndexKeys
                .Ascending(b => b.UserId)
                .Ascending(b => b.Month)
        ));
    }

    public async Task TestConnectionAsync()
    {
        // Run a ping command to verify connectivity
        try
        {
            var result = await _database.RunCommandAsync((Command<BsonDocument>)"{ping:1}");
        }
        catch (Exception ex)
        {
            // Throw full exception details to aid diagnostics (will be logged by caller)
            throw new Exception("MongoDB TestConnectionAsync failed: " + ex.ToString(), ex);
        }
    }

    public async Task CreateIndexesAsync()
    {
        await CreateCollectionsAndIndexesAsync();
    }
}
