using MoneyManager.Infrastructure.Data;
using Xunit;

namespace MoneyManager.Tests.Infrastructure.Data;

public class MongoContextIndexTests
{
    [Fact]
    public void BuildInvoiceImportFilesTtlIndex_ShouldExpireFilesAfter24Hours()
    {
        // Act
        var index = MongoContext.BuildInvoiceImportFilesTtlIndex();

        // Assert
        Assert.Equal(TimeSpan.FromHours(24), MongoContext.InvoiceImportFilesTtl);
        Assert.Equal(TimeSpan.FromHours(24), index.Options.ExpireAfter);
        Assert.Equal("createdAt_ttl", index.Options.Name);
    }
}
