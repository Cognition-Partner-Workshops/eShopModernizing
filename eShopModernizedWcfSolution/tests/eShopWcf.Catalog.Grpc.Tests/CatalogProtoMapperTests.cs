using System.Globalization;
using eShop.Catalog.Grpc.Protos;
using eShopWcf.Catalog.Grpc.Mapping;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace eShopWcf.Catalog.Grpc.Tests;

public sealed class CatalogProtoMapperTests
{
    [Theory]
    [InlineData("19.5")]
    [InlineData("8.50")]
    [InlineData("12.3456")]
    [InlineData("-1.25")]
    [InlineData("0")]
    [InlineData("922337203685477.5807")]
    public void DecimalValueRoundTripsExactly(string literal)
    {
        var value = decimal.Parse(literal, CultureInfo.InvariantCulture);
        Assert.Equal(value, CatalogProtoMapper.ToDecimal(CatalogProtoMapper.ToDecimalValue(value)));
    }

    [Theory]
    [InlineData(0, 1000000000)]
    [InlineData(0, -1000000000)]
    [InlineData(1, -1)]
    [InlineData(-1, 1)]
    public void RejectsInvalidDecimal(long units, int nanos)
    {
        var exception = Assert.Throws<RpcException>(() => CatalogProtoMapper.ToDecimal(new DecimalValue { Units = units, Nanos = nanos }));
        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }

    [Fact]
    public void TimestampRoundTripsOnlyTheDateComponent()
    {
        var value = new DateTime(2017, 9, 21, 23, 45, 0, DateTimeKind.Unspecified);
        var timestamp = CatalogProtoMapper.ToTimestamp(value);
        Assert.Equal(DateTimeKind.Utc, timestamp.ToDateTime().Kind);
        var roundTrip = CatalogProtoMapper.ToDateTime(timestamp);
        Assert.Equal(value.Date, roundTrip);
        Assert.Equal(DateTimeKind.Unspecified, roundTrip.Kind);
        Assert.Equal(value.Date, CatalogProtoMapper.ToDateTime(Timestamp.FromDateTime(DateTime.SpecifyKind(value, DateTimeKind.Utc))));
    }

    [Fact]
    public void MissingAndInvalidTimestampAreInvalidArguments()
    {
        Assert.Equal(StatusCode.InvalidArgument, Assert.Throws<RpcException>(() => CatalogProtoMapper.ToDateTime(null)).StatusCode);
        Assert.Equal(StatusCode.InvalidArgument, Assert.Throws<RpcException>(() =>
            CatalogProtoMapper.ToDateTime(new Timestamp { Seconds = long.MaxValue })).StatusCode);
    }

    [Fact]
    public void NullStringsAndNavigationsUseProtoDefaultsAndEmptyPictureIsNotSubstituted()
    {
        var proto = CatalogProtoMapper.ToProto(new Data.CatalogItem());
        Assert.Equal("", proto.Name);
        Assert.Null(proto.CatalogBrand);
        Assert.Null(proto.CatalogType);
        Assert.Equal("", CatalogProtoMapper.ToDomain(proto).Picturefilename);
        Assert.Equal(0M, CatalogProtoMapper.ToDecimal(null));
    }
}
