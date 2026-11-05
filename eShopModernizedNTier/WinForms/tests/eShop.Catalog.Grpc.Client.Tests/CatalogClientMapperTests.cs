using Xunit;

namespace eShop.Catalog.Grpc.Client.Tests;

public class CatalogClientMapperTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("19.5000")]
    [InlineData("0.0001")]
    [InlineData("-19.5001")]
    [InlineData("1000000.9999")]
    public void DecimalRoundTripPreservesExactValue(string text)
    {
        var value = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(value, CatalogClientMapper.ToDecimal(CatalogClientMapper.ToDecimalValue(value)));
    }
}
