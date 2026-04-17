using MongoDB.Driver;

namespace MoneyManager.Infrastructure.Data.Migrations;

public class Migration_20260416_01_CreditCards : IMigration
{
    public string Id => "20260416_01_CreditCards";
    public string Description => "Introduce credit_cards, credit_card_invoices and credit_card_transactions collections.";

    public async Task ExecuteAsync(IMongoDatabase database)
    {
        var collections = await (await database.ListCollectionNamesAsync()).ToListAsync();

        foreach (var name in new[] { "credit_cards", "credit_card_invoices", "credit_card_transactions" })
        {
            if (!collections.Contains(name))
            {
                await database.CreateCollectionAsync(name);
            }
        }
    }
}
