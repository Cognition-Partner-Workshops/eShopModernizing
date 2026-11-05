using System.Net;
using eShopWinForms.Grpc.Client;
using eShopWinForms.Grpc.Client.Models;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Reference = eShop.Catalog.Grpc.Protos;

namespace eShopWinForms.Tests;

public sealed class ClientTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private CatalogServiceGrpcClient _client = null!;
    private string _address = null!;
    private readonly FakeCatalog _fake = new();

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(o => o.Listen(IPAddress.Loopback, 0, l => l.Protocols = HttpProtocols.Http2));
        builder.Services.AddGrpc(o => o.Interceptors.Add<DeadlineInterceptor>());
        builder.Services.AddSingleton(_fake);
        _app = builder.Build();
        _app.MapGrpcService<FakeCatalog>();
        await _app.StartAsync();
        _address = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        _client = new CatalogServiceGrpcClient(new CatalogServiceClientOptions { Address = _address });
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public void ReadsMapNestedModelsExactPriceFiltersAndDate()
    {
        var item = Assert.Single(_client.GetCatalogItems(2, 1));
        Assert.Equal((2, 1), _fake.Filters);
        Assert.Equal(19.5001m, item.Price);
        Assert.Equal("2.png", item.Picturefilename);
        Assert.Equal(".NET", item.CatalogBrand!.Brand);
        Assert.Equal("Mug", item.CatalogType!.Type);
        Assert.Equal(1, _client.FindCatalogItem(1)!.Id);
        Assert.Equal(".NET", Assert.Single(_client.GetCatalogBrands()).Brand);
        Assert.Equal("Mug", Assert.Single(_client.GetCatalogTypes()).Type);
        var day = new DateTime(2026, 11, 5);
        var discount = _client.GetDiscount(day)!;
        Assert.Equal(7, discount.Id);
        Assert.Equal(.15, discount.Size);
        Assert.Equal(DateTimeKind.Unspecified, discount.Start.Kind);
        Assert.Equal(day, _fake.Day);
        Assert.Equal(100, _client.GetAvailableStock(day, 1));
        Assert.Equal(1, _fake.StockItem);
        Assert.Equal(TimeSpan.FromMinutes(1), new CatalogServiceClientOptions().CallTimeout);
        Assert.InRange(_fake.LastDeadline, DateTime.UtcNow.AddSeconds(50), DateTime.UtcNow.AddSeconds(61));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveTimeoutIsRejected(int milliseconds)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CatalogServiceGrpcClient(
            new CatalogServiceClientOptions { Address = _address, CallTimeout = TimeSpan.FromMilliseconds(milliseconds) }));
    }

    [Theory]
    [InlineData("find")]
    [InlineData("brands")]
    [InlineData("types")]
    [InlineData("items")]
    [InlineData("stock")]
    [InlineData("create-stock")]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("remove")]
    [InlineData("discount")]
    public async Task EveryRpcTimesOutWhenServiceNeverResponds(string operation)
    {
        _client.Dispose();
        _client = new CatalogServiceGrpcClient(new CatalogServiceClientOptions
        {
            Address = _address,
            CallTimeout = TimeSpan.FromMilliseconds(500)
        });
        _fake.NeverRespond = true;
        var exception = await Task.Run(() => Assert.Throws<RpcException>(() =>
        {
            switch (operation)
            {
                case "find": _client.FindCatalogItem(1); break;
                case "brands": _client.GetCatalogBrands(); break;
                case "types": _client.GetCatalogTypes(); break;
                case "items": _client.GetCatalogItems(0, 0); break;
                case "stock": _client.GetAvailableStock(DateTime.Today, 1); break;
                case "create-stock": _client.CreateAvailableStock(new()); break;
                case "create": _client.CreateCatalogItem(new()); break;
                case "update": _client.UpdateCatalogItem(new()); break;
                case "remove": _client.RemoveCatalogItem(new()); break;
                case "discount": _client.GetDiscount(DateTime.Today); break;
                default: throw new ArgumentOutOfRangeException(nameof(operation));
            }
        })).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(StatusCode.DeadlineExceeded, exception.StatusCode);
    }

    [Fact]
    public void MissingResponsesAndNotFoundTranslateToLegacyNull()
    {
        _fake.Missing = true;
        Assert.Null(_client.FindCatalogItem(999));
        Assert.Null(_client.GetDiscount(DateTime.Today));
        Assert.Empty(_client.GetCatalogItems(0, 0));
        Assert.Empty(_client.GetCatalogBrands());
        Assert.Empty(_client.GetCatalogTypes());
        Assert.Equal(0, _client.GetAvailableStock(DateTime.Today, 3));
        _fake.EmptyMessage = true;
        Assert.Null(_client.FindCatalogItem(999));
        Assert.Null(_client.GetDiscount(DateTime.Today));
    }

    [Fact]
    public void AllWritesMapPayloadAndPreserveDecimalAndTimestamp()
    {
        var item = new CatalogItem
        {
            Id = 12,
            Name = "Cup<T> TShirt",
            Description = "description",
            Picturefilename = "4.png",
            Price = -12.3456m,
            CatalogBrandId = 5,
            CatalogTypeId = 2,
            CatalogBrand = new() { Id = 5, Brand = "Other" },
            CatalogType = new() { Id = 2, Type = "T-Shirt" }
        };
        _client.CreateCatalogItem(item);
        Assert.Equal("create", _fake.Operation);
        AssertPayload(item);
        _client.UpdateCatalogItem(item);
        Assert.Equal("update", _fake.Operation);
        AssertPayload(item);
        _client.RemoveCatalogItem(item);
        Assert.Equal("remove", _fake.Operation);
        AssertPayload(item);
        var date = new DateTime(2030, 1, 15, 12, 30, 0, DateTimeKind.Unspecified);
        _client.CreateAvailableStock(new() { StockId = 6, CatalogItemId = 3, Date = date, AvailableStock = 75 });
        Assert.Equal(6, _fake.Stock!.StockId);
        Assert.Equal(3, _fake.Stock.CatalogItemId);
        Assert.Equal(75, _fake.Stock.AvailableStock);
        Assert.Equal(DateTime.SpecifyKind(date, DateTimeKind.Utc), _fake.Stock.Date.ToDateTime());
    }

    private void AssertPayload(CatalogItem expected)
    {
        var actual = _fake.Item!;
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.Picturefilename, actual.PictureFilename);
        Assert.Equal(expected.Price, actual.Price.Units + actual.Price.Nanos / 1000000000m);
        Assert.Equal(expected.CatalogBrandId, actual.CatalogBrandId);
        Assert.Equal(expected.CatalogTypeId, actual.CatalogTypeId);
        Assert.Equal("Other", actual.CatalogBrand.Brand);
        Assert.Equal("T-Shirt", actual.CatalogType.Type);
    }

    [Fact]
    public void NonNotFoundErrorsPropagate()
    {
        _fake.Fail = true;
        Assert.Equal(StatusCode.Unavailable, Assert.Throws<RpcException>(() => _client.FindCatalogItem(1)).StatusCode);
        Assert.Equal(StatusCode.Unavailable, Assert.Throws<RpcException>(() => _client.GetDiscount(DateTime.Today)).StatusCode);
        Assert.Equal(StatusCode.Unavailable, Assert.Throws<RpcException>(() => _client.GetCatalogItems(0, 0)).StatusCode);
        Assert.Throws<RpcException>(() => _client.CreateCatalogItem(new()));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("19.5001")]
    [InlineData("-12.3456")]
    [InlineData("922337203685477.5807")]
    public void DecimalConversionIsExact(string text)
    {
        var value = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(value, CatalogClientMapper.ToDecimal(CatalogClientMapper.ToDecimalValue(value)));
    }

    [Theory]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Utc)]
    public void DateConversionReinterpretsWithoutTimezoneShift(DateTimeKind kind)
    {
        var value = new DateTime(2017, 9, 20, 0, 0, 0, kind);
        var roundtrip = CatalogClientMapper.ToDateTime(CatalogClientMapper.ToTimestamp(value));
        Assert.Equal(value.Ticks, roundtrip.Ticks);
        Assert.Equal(DateTimeKind.Unspecified, roundtrip.Kind);
    }

    [Fact]
    public void NullMapperInputsUseLegacyDefaults()
    {
        Assert.Equal(0m, CatalogClientMapper.ToDecimal(null));
        Assert.Equal(default, CatalogClientMapper.ToDateTime(null));
    }

    public sealed class DeadlineInterceptor : Interceptor
    {
        private readonly FakeCatalog _fake;
        public DeadlineInterceptor(FakeCatalog fake) => _fake = fake;

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request,
            ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            _fake.LastDeadline = context.Deadline;
            if (_fake.NeverRespond) await Task.Delay(Timeout.Infinite, context.CancellationToken);
            return await continuation(request, context);
        }
    }

    public sealed class FakeCatalog : Reference.Catalog.CatalogBase
    {
        public bool NeverRespond { get; set; }
        public DateTime LastDeadline { get; set; }
        public bool Missing { get; set; }
        public bool EmptyMessage { get; set; }
        public bool Fail { get; set; }
        public (int, int) Filters { get; private set; }
        public DateTime Day { get; private set; }
        public int StockItem { get; private set; }
        public Reference.CatalogItem? Item { get; private set; }
        public Reference.CatalogItemsStock? Stock { get; private set; }
        public string? Operation { get; private set; }
        private void Check() { if (Fail) throw new RpcException(new Status(StatusCode.Unavailable, "test failure")); }
        private static Reference.CatalogItem Sample() => new()
        {
            Id = 1,
            Name = "Hoodie",
            Description = "description",
            PictureFilename = "2.png",
            Price = new() { Units = 19, Nanos = 500100000 },
            CatalogBrandId = 2,
            CatalogTypeId = 1,
            CatalogBrand = new() { Id = 2, Brand = ".NET" },
            CatalogType = new() { Id = 1, Type = "Mug" }
        };

        public override Task<Reference.FindCatalogItemResponse> FindCatalogItem(Reference.FindCatalogItemRequest request, ServerCallContext context)
        {
            Check();
            if (EmptyMessage) return Task.FromResult(new Reference.FindCatalogItemResponse());
            if (Missing) throw new RpcException(new Status(StatusCode.NotFound, "missing"));
            return Task.FromResult(new Reference.FindCatalogItemResponse { Item = Sample() });
        }
        public override Task<Reference.GetCatalogItemsResponse> GetCatalogItems(Reference.GetCatalogItemsRequest request, ServerCallContext context)
        {
            Check(); Filters = (request.BrandIdFilter, request.TypeIdFilter);
            var response = new Reference.GetCatalogItemsResponse();
            if (!Missing) response.Items.Add(Sample());
            return Task.FromResult(response);
        }
        public override Task<Reference.GetCatalogBrandsResponse> GetCatalogBrands(Reference.GetCatalogBrandsRequest request, ServerCallContext context)
        {
            var response = new Reference.GetCatalogBrandsResponse();
            if (!Missing) response.Brands.Add(new Reference.CatalogBrand { Id = 2, Brand = ".NET" });
            return Task.FromResult(response);
        }
        public override Task<Reference.GetCatalogTypesResponse> GetCatalogTypes(Reference.GetCatalogTypesRequest request, ServerCallContext context)
        {
            var response = new Reference.GetCatalogTypesResponse();
            if (!Missing) response.Types_.Add(new Reference.CatalogType { Id = 1, Type = "Mug" });
            return Task.FromResult(response);
        }
        public override Task<Reference.GetDiscountResponse> GetDiscount(Reference.GetDiscountRequest request, ServerCallContext context)
        {
            Check(); Day = DateTime.SpecifyKind(request.Day.ToDateTime(), DateTimeKind.Unspecified);
            if (EmptyMessage) return Task.FromResult(new Reference.GetDiscountResponse());
            if (Missing) throw new RpcException(new Status(StatusCode.NotFound, "missing"));
            return Task.FromResult(new Reference.GetDiscountResponse
            {
                Discount = new() { Id = 7, Size = .15, Start = request.Day, End = request.Day }
            });
        }
        public override Task<Reference.GetAvailableStockResponse> GetAvailableStock(Reference.GetAvailableStockRequest request, ServerCallContext context)
        {
            StockItem = request.CatalogItemId;
            Day = DateTime.SpecifyKind(request.Date.ToDateTime(), DateTimeKind.Unspecified);
            return Task.FromResult(new Reference.GetAvailableStockResponse { AvailableStock = Missing ? 0 : 100 });
        }
        public override Task<Reference.CreateAvailableStockResponse> CreateAvailableStock(Reference.CreateAvailableStockRequest request, ServerCallContext context)
        {
            Check(); Stock = request.CatalogItemsStock;
            return Task.FromResult(new Reference.CreateAvailableStockResponse());
        }
        public override Task<Reference.CreateCatalogItemResponse> CreateCatalogItem(Reference.CreateCatalogItemRequest request, ServerCallContext context)
        {
            Check(); Operation = "create"; Item = request.CatalogItem;
            return Task.FromResult(new Reference.CreateCatalogItemResponse());
        }
        public override Task<Reference.UpdateCatalogItemResponse> UpdateCatalogItem(Reference.UpdateCatalogItemRequest request, ServerCallContext context)
        {
            Check(); Operation = "update"; Item = request.CatalogItem;
            return Task.FromResult(new Reference.UpdateCatalogItemResponse());
        }
        public override Task<Reference.RemoveCatalogItemResponse> RemoveCatalogItem(Reference.RemoveCatalogItemRequest request, ServerCallContext context)
        {
            Check(); Operation = "remove"; Item = request.CatalogItem;
            return Task.FromResult(new Reference.RemoveCatalogItemResponse());
        }
    }
}
