using MongoDB.Bson;
using MongoDB.Driver;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Infrastructure.Data.Migrations;

public class Migration_20260401_01_CreditCardCommittedCreditDefaults : IMigration
{
    public string Id => "20260401_01_CreditCardCommittedCreditDefaults";

    public string Description => "Initialize committedCredit for existing credit card accounts";

    public async Task ExecuteAsync(IMongoDatabase database)
    {
        var accounts = database.GetCollection<BsonDocument>("accounts");
        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Or(
                Builders<BsonDocument>.Filter.Eq("type", (int)AccountType.CreditCard),
                Builders<BsonDocument>.Filter.Eq("type", AccountType.CreditCard.ToString())),
            Builders<BsonDocument>.Filter.Or(
                Builders<BsonDocument>.Filter.Exists("committedCredit", false),
                Builders<BsonDocument>.Filter.Eq("committedCredit", BsonNull.Value)));

        var documents = await accounts.Find(filter).ToListAsync();

        foreach (var document in documents)
        {
            var balance = document.TryGetValue("balance", out var balanceValue) && balanceValue.IsNumeric
                ? balanceValue.ToDecimal()
                : 0m;

            var committedCredit = Math.Abs(balance);

            await accounts.UpdateOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", document["_id"]),
                Builders<BsonDocument>.Update.Set("committedCredit", committedCredit));
        }
    }
}