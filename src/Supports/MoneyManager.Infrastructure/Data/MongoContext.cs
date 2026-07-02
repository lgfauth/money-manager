using MongoDB.Driver;
using MongoDB.Bson;
using System.Security.Authentication;
using MoneyManager.Infrastructure.Data.Migrations;

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
        await transactionsCollection.Indexes.CreateOneAsync(new CreateIndexModel<MoneyManager.Domain.Entities.Transaction>(
            Builders<MoneyManager.Domain.Entities.Transaction>.IndexKeys
                .Ascending(t => t.UserId)
                .Ascending(t => t.AccountId)
        ));
        await transactionsCollection.Indexes.CreateOneAsync(new CreateIndexModel<MoneyManager.Domain.Entities.Transaction>(
            Builders<MoneyManager.Domain.Entities.Transaction>.IndexKeys
                .Ascending(t => t.UserId)
                .Ascending(t => t.CategoryId)
        ));
        await transactionsCollection.Indexes.CreateOneAsync(new CreateIndexModel<MoneyManager.Domain.Entities.Transaction>(
            Builders<MoneyManager.Domain.Entities.Transaction>.IndexKeys
                .Ascending(t => t.UserId)
                .Ascending(t => t.IsDeleted)
        ));

        // Replace legacy sparse indexes with partial indexes that ignore nulls safely.
        await DropIndexIfExistsAsync(transactionsCollection, "userId_1_clientRequestId_1");
        await transactionsCollection.Indexes.CreateOneAsync(new CreateIndexModel<MoneyManager.Domain.Entities.Transaction>(
            Builders<MoneyManager.Domain.Entities.Transaction>.IndexKeys
                .Ascending(t => t.UserId)
                .Ascending(t => t.ClientRequestId),
            new CreateIndexOptions<MoneyManager.Domain.Entities.Transaction>
            {
                Unique = true,
                PartialFilterExpression = new BsonDocument
                {
                    {
                        "clientRequestId",
                        new BsonDocument
                        {
                            { "$type", "string" }
                        }
                    }
                }
            }
        ));

        await DropIndexIfExistsAsync(transactionsCollection, "userId_1_externalId_1");
        await transactionsCollection.Indexes.CreateOneAsync(new CreateIndexModel<MoneyManager.Domain.Entities.Transaction>(
            Builders<MoneyManager.Domain.Entities.Transaction>.IndexKeys
                .Ascending(t => t.UserId)
                .Ascending(t => t.ExternalId),
            new CreateIndexOptions<MoneyManager.Domain.Entities.Transaction>
            {
                Unique = true,
                PartialFilterExpression = new BsonDocument
                {
                    {
                        "externalId",
                        new BsonDocument
                        {
                            { "$type", "string" }
                        }
                    }
                }
            }
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

        // Create bank_connections collection
        if (!collectionNames.Contains("bank_connections"))
        {
            await _database.CreateCollectionAsync("bank_connections");
        }
        var bankConnectionsCollection = _database.GetCollection<MoneyManager.Domain.Entities.BankConnection>("bank_connections");
        await bankConnectionsCollection.Indexes.CreateOneAsync(new CreateIndexModel<MoneyManager.Domain.Entities.BankConnection>(
            Builders<MoneyManager.Domain.Entities.BankConnection>.IndexKeys
                .Ascending(c => c.UserId)
                .Ascending(c => c.IsDeleted)
        ));
        await bankConnectionsCollection.Indexes.CreateOneAsync(new CreateIndexModel<MoneyManager.Domain.Entities.BankConnection>(
            Builders<MoneyManager.Domain.Entities.BankConnection>.IndexKeys
                .Ascending(c => c.ExternalConnectionId)
        ));
        await bankConnectionsCollection.Indexes.CreateOneAsync(new CreateIndexModel<MoneyManager.Domain.Entities.BankConnection>(
            Builders<MoneyManager.Domain.Entities.BankConnection>.IndexKeys
                .Ascending(c => c.Status)
                .Ascending(c => c.IsDeleted)
        ));

        // Create push_subscriptions collection
        if (!collectionNames.Contains("push_subscriptions"))
        {
            await _database.CreateCollectionAsync("push_subscriptions");
        }
        var pushSubsCollection = _database.GetCollection<MoneyManager.Domain.Entities.PushSubscription>("push_subscriptions");
        await pushSubsCollection.Indexes.CreateOneAsync(new CreateIndexModel<MoneyManager.Domain.Entities.PushSubscription>(
            Builders<MoneyManager.Domain.Entities.PushSubscription>.IndexKeys
                .Ascending(s => s.UserId)
        ));
        await pushSubsCollection.Indexes.CreateOneAsync(new CreateIndexModel<MoneyManager.Domain.Entities.PushSubscription>(
            Builders<MoneyManager.Domain.Entities.PushSubscription>.IndexKeys
                .Ascending(s => s.Endpoint),
            new CreateIndexOptions { Unique = true, Sparse = true }
        ));

        // Create user_reports collection
        if (!collectionNames.Contains("user_reports"))
        {
            await _database.CreateCollectionAsync("user_reports");
        }
        var userReportsCollection = _database.GetCollection<MoneyManager.Domain.Entities.UserReport>("user_reports");
        await userReportsCollection.Indexes.CreateOneAsync(new CreateIndexModel<MoneyManager.Domain.Entities.UserReport>(
            Builders<MoneyManager.Domain.Entities.UserReport>.IndexKeys
                .Ascending(r => r.UserId)
                .Descending(r => r.CreatedAt)
        ));

        // Create credit_cards collection
        if (!collectionNames.Contains("credit_cards"))
        {
            await _database.CreateCollectionAsync("credit_cards");
        }
        var creditCardsCollection = _database.GetCollection<MoneyManager.Domain.Entities.CreditCard>("credit_cards");
        await creditCardsCollection.Indexes.CreateOneAsync(new CreateIndexModel<MoneyManager.Domain.Entities.CreditCard>(
            Builders<MoneyManager.Domain.Entities.CreditCard>.IndexKeys
                .Ascending(c => c.UserId)
                .Ascending(c => c.IsDeleted)
        ));

        // Create credit_card_invoices collection
        if (!collectionNames.Contains("credit_card_invoices"))
        {
            await _database.CreateCollectionAsync("credit_card_invoices");
        }
        var creditCardInvoicesCollection = _database.GetCollection<MoneyManager.Domain.Entities.CreditCardInvoice>("credit_card_invoices");
        await creditCardInvoicesCollection.Indexes.CreateOneAsync(new CreateIndexModel<MoneyManager.Domain.Entities.CreditCardInvoice>(
            Builders<MoneyManager.Domain.Entities.CreditCardInvoice>.IndexKeys
                .Ascending(i => i.UserId)
                .Ascending(i => i.CreditCardId)
                .Ascending(i => i.ReferenceMonth),
            new CreateIndexOptions { Unique = true, Sparse = true }
        ));
        await creditCardInvoicesCollection.Indexes.CreateOneAsync(new CreateIndexModel<MoneyManager.Domain.Entities.CreditCardInvoice>(
            Builders<MoneyManager.Domain.Entities.CreditCardInvoice>.IndexKeys
                .Ascending(i => i.UserId)
                .Ascending(i => i.Status)
        ));

        // Create credit_card_transactions collection
        if (!collectionNames.Contains("credit_card_transactions"))
        {
            await _database.CreateCollectionAsync("credit_card_transactions");
        }
        var creditCardTransactionsCollection = _database.GetCollection<MoneyManager.Domain.Entities.CreditCardTransaction>("credit_card_transactions");
        await creditCardTransactionsCollection.Indexes.CreateOneAsync(new CreateIndexModel<MoneyManager.Domain.Entities.CreditCardTransaction>(
            Builders<MoneyManager.Domain.Entities.CreditCardTransaction>.IndexKeys
                .Ascending(t => t.UserId)
                .Ascending(t => t.InvoiceId)
        ));
        await creditCardTransactionsCollection.Indexes.CreateOneAsync(new CreateIndexModel<MoneyManager.Domain.Entities.CreditCardTransaction>(
            Builders<MoneyManager.Domain.Entities.CreditCardTransaction>.IndexKeys
                .Ascending(t => t.UserId)
                .Ascending(t => t.CreditCardId)
        ));
        await creditCardTransactionsCollection.Indexes.CreateOneAsync(new CreateIndexModel<MoneyManager.Domain.Entities.CreditCardTransaction>(
            Builders<MoneyManager.Domain.Entities.CreditCardTransaction>.IndexKeys
                .Ascending(t => t.UserId)
                .Ascending(t => t.ParentTransactionId)
        ));
        await creditCardTransactionsCollection.Indexes.CreateOneAsync(new CreateIndexModel<MoneyManager.Domain.Entities.CreditCardTransaction>(
            Builders<MoneyManager.Domain.Entities.CreditCardTransaction>.IndexKeys
                .Ascending(t => t.UserId)
                .Ascending(t => t.ExternalId),
            new CreateIndexOptions { Sparse = true }
        ));

        // Create subscriptions collection
        if (!collectionNames.Contains("subscriptions"))
        {
            await _database.CreateCollectionAsync("subscriptions");
        }
        var subscriptionsCollection = _database.GetCollection<MoneyManager.Domain.Entities.Subscription>("subscriptions");
        await subscriptionsCollection.Indexes.CreateOneAsync(new CreateIndexModel<MoneyManager.Domain.Entities.Subscription>(
            Builders<MoneyManager.Domain.Entities.Subscription>.IndexKeys.Ascending(s => s.UserId),
            new CreateIndexOptions { Unique = true, Sparse = true }
        ));
        await subscriptionsCollection.Indexes.CreateOneAsync(new CreateIndexModel<MoneyManager.Domain.Entities.Subscription>(
            Builders<MoneyManager.Domain.Entities.Subscription>.IndexKeys.Ascending(s => s.ExternalSubscriptionId),
            new CreateIndexOptions { Sparse = true }
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

    public async Task RunMigrationsAsync()
    {
        var runner = new MigrationRunner(_database);
        var migrations = new IMigration[]
        {
            new Migration_20260326_01_Initial(),
            new Migration_20260416_01_CreditCards()
        };

        await runner.RunAsync(migrations);
    }

    public async Task CreateIndexesAsync()
    {
        await CreateCollectionsAndIndexesAsync();
    }

    private static async Task DropIndexIfExistsAsync<T>(IMongoCollection<T> collection, string indexName)
    {
        try
        {
            await collection.Indexes.DropOneAsync(indexName);
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexNotFound")
        {
            // Index doesn't exist yet; nothing to migrate.
        }
    }
}
