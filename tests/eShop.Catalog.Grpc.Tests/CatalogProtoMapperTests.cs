using eShop.Catalog.Grpc.Mapping;
using Xunit;

namespace eShop.Catalog.Grpc.Tests;

/// <summary>
/// The decimal mapping is the one place where the SOAP contract could silently lose precision, so
/// it is pinned separately: <c>CatalogItem.Price</c> is <c>money</c> / decimal(19,4) in the legacy
/// schema and must survive the round trip exactly.
/// </summary>
public class CatalogProtoMapperTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("8.5")]
    [InlineData("19.5")]
    [InlineData("12.3456")]
    [InlineData("-4.75")]
    [InlineData("999999.9999")]
    public void DecimalValue_RoundTripsExactly(string literal)
    {
        var value = decimal.Parse(literal, System.Globalization.CultureInfo.InvariantCulture);

        var roundTripped = CatalogProtoMapper.ToDecimal(CatalogProtoMapper.ToDecimalValue(value));

        Assert.Equal(value, roundTripped);
    }

    [Fact]
    public void Timestamp_RoundTripsTheDateComponent()
    {
        var date = new DateTime(2017, 9, 21);

        var roundTripped = CatalogProtoMapper.ToDateTime(CatalogProtoMapper.ToTimestamp(date));

        Assert.Equal(date, roundTripped);
        Assert.Equal(DateTimeKind.Unspecified, roundTripped.Kind);
    }
}
