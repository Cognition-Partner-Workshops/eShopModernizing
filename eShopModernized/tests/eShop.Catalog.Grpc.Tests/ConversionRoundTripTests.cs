using System.Globalization;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using DomainDiscountItem = eShop.Catalog.Domain.Entities.DiscountItem;

namespace eShop.Catalog.Grpc.Tests;

/// <summary>
/// Round-trip coverage for the two non-native type mappings in <c>catalog.proto</c>:
/// <c>decimal</c> as a string-encoded <c>DecimalValue</c> and <c>DateTime</c> as
/// <c>google.protobuf.Timestamp</c>.
/// </summary>
public class ConversionRoundTripTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("0.01")]
    [InlineData("8.5")]
    [InlineData("19.5")]
    [InlineData("999999.99")]
    public async Task DecimalValue_RoundTripsWithoutPrecisionLoss(string price)
    {
        using var app = new CatalogGrpcApplication();
        app.Seed();

        await app.Client.CreateCatalogItemAsync(new CatalogItem
        {
            Name = "Round trip",
            Price = new DecimalValue { Value = price },
            CatalogBrandId = 1,
            CatalogTypeId = 1,
        });

        var stored = await app.Client.FindCatalogItemAsync(new FindCatalogItemRequest { Id = 13 });

        // Value equality, not string equality: the stored scale is whatever the provider keeps
        // (SQLite renders 0 as "0.0"), but the number itself must survive untouched.
        Assert.Equal(
            decimal.Parse(price, CultureInfo.InvariantCulture),
            decimal.Parse(stored.Price.Value, CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task DecimalValue_UsesTheInvariantCultureRegardlessOfTheAmbientCulture()
    {
        var originalCulture = CultureInfo.DefaultThreadCurrentCulture;
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("de-DE");

        try
        {
            using var app = new CatalogGrpcApplication();
            app.Seed();

            var item = await app.Client.FindCatalogItemAsync(new FindCatalogItemRequest { Id = 1 });

            Assert.Equal("19.5", item.Price.Value);
        }
        finally
        {
            CultureInfo.DefaultThreadCurrentCulture = originalCulture;
        }
    }

    [Fact]
    public async Task DecimalValue_ThatIsNotAnInvariantDecimal_IsInvalidArgument()
    {
        using var app = new CatalogGrpcApplication();
        app.Seed();

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => app.Client.CreateCatalogItemAsync(new CatalogItem
            {
                Name = "Comma separated",
                Price = new DecimalValue { Value = "19,5" },
                CatalogBrandId = 1,
                CatalogTypeId = 1,
            }).ResponseAsync);

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }

    /// <summary>
    /// The legacy service compared <c>DateTime.Date</c> values against SQL <c>date</c> columns, so
    /// a timestamp carrying a time of day must resolve to the same row as midnight on that day.
    /// </summary>
    [Fact]
    public async Task Timestamp_RoundTripsAsADateAndIgnoresTheTimeOfDay()
    {
        using var app = new CatalogGrpcApplication();
        app.Seed();

        await app.Client.CreateAvailableStockAsync(new CatalogItemsStock
        {
            CatalogItemId = 3,
            AvailableStock = 11,
            Date = Timestamp.FromDateTime(new DateTime(2026, 5, 4, 13, 45, 30, DateTimeKind.Utc)),
        });

        using (var context = app.CreateContext())
        {
            var stock = await context.CatalogItemsStocks.SingleAsync();
            Assert.Equal(new DateTime(2026, 5, 4, 0, 0, 0, DateTimeKind.Unspecified), stock.Date);
        }

        var response = await app.Client.GetAvailableStockAsync(new GetAvailableStockRequest
        {
            CatalogItemId = 3,
            Date = Timestamp.FromDateTime(new DateTime(2026, 5, 4, 23, 59, 59, DateTimeKind.Utc)),
        });

        Assert.Equal(11, response.AvailableStock);
    }

    [Fact]
    public async Task Timestamp_IsEmittedAsUtcMidnight()
    {
        using var app = new CatalogGrpcApplication();
        app.Seed();
        app.Seed(new DomainDiscountItem
        {
            Id = 1,
            Size = 0.2,
            Start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
            End = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Unspecified),
        });

        var discount = await app.Client.GetDiscountAsync(new GetDiscountRequest
        {
            Day = Timestamp.FromDateTime(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)),
        });

        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), discount.Start.ToDateTime());
        Assert.Equal(DateTimeKind.Utc, discount.End.ToDateTime().Kind);
    }
}
