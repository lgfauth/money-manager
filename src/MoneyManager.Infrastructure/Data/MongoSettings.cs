namespace MoneyManager.Infrastructure.Data;

public class MongoSettings
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "moneymanager";
}
