using eShopWinForms.Controllers;
using eShopWinForms.Grpc.Client;
using eShopWinForms.Grpc.Client.Models;
using Xunit;

namespace eShopWinForms.Tests;

public sealed class ControllerTests
{
    [Fact]
    public void LoadInitializesFiltersItemsShipmentAndLegacyBanner()
    {
        var service = new FakeClient { Discount = new DiscountItem { Size = .15, End = new DateTime(2030, 12, 31) } };
        var view = new FakeView();
        var controller = new CatalogController(service, view);
        controller.LoadView();
        Assert.Same(controller, view.Controller);
        Assert.Equal("All", view.Brands[0]);
        Assert.Equal(".NET", view.Brands[2]);
        Assert.Equal("All", view.Types[0]);
        Assert.Equal("Mug", view.Types[1]);
        Assert.Equal(service.Items, view.Items);
        Assert.Equal(service.Items, view.Shipment);
        Assert.Equal(.15, view.Discount);
        Assert.Equal($"15% sale endson {service.Discount.End.ToShortDateString()}!", view.Banner);
        Assert.Equal(1, view.Clears);
    }

    [Fact]
    public void EmptyCatalogAndMissingDiscountArePassedThrough()
    {
        var service = new FakeClient { Items = [] };
        var view = new FakeView();
        new CatalogController(service, view).LoadView();
        Assert.Empty(view.Items);
        Assert.Empty(view.Shipment);
        Assert.Null(view.Banner);
        Assert.Equal(0, view.Discount);
    }

    [Theory]
    [InlineData(2, 0)]
    [InlineData(0, 2)]
    [InlineData(5, 1)]
    public void FilterEventPreservesBrandAndTypeOrder(int brand, int type)
    {
        var service = new FakeClient();
        var view = new FakeView();
        _ = new CatalogController(service, view);
        view.Filter(brand, type);
        Assert.Equal((brand, type), service.Filters);
        Assert.Equal(1, view.Clears);
    }

    [Fact]
    public void ShipmentEventWritesBeforeNotifying()
    {
        var service = new FakeClient();
        var view = new FakeView();
        _ = new CatalogController(service, view);
        var date = new DateTime(2030, 1, 15);
        view.Add(3, 50, date);
        Assert.Equal(3, service.Stock!.CatalogItemId);
        Assert.Equal(50, service.Stock.AvailableStock);
        Assert.Equal(date, service.Stock.Date);
        Assert.Equal(1, view.Notifications);
    }

    [Fact]
    public void SearchAppendsResultThroughView()
    {
        var view = new FakeView();
        _ = new CatalogController(new FakeClient(), view);
        var date = new DateTime(2017, 9, 20);
        view.Search(1, date);
        Assert.Equal(100, view.Available);
        Assert.Equal(date, view.SearchArgs!.date);
        Assert.Equal(1, view.SearchArgs.itemId);
    }

    [Fact]
    public void ServiceFailurePropagatesWithoutNotification()
    {
        var view = new FakeView();
        _ = new CatalogController(new FakeClient { FailWrites = true }, view);
        Assert.Throws<InvalidOperationException>(() => view.Add(1, 5, DateTime.Today));
        Assert.Equal(0, view.Notifications);
    }

    private sealed class FakeClient : ICatalogServiceClient
    {
        public IReadOnlyList<CatalogItem> Items { get; set; } = [new() { Id = 1, Name = "Item" }];
        public DiscountItem? Discount { get; set; }
        public (int, int) Filters { get; private set; }
        public CatalogItemsStock? Stock { get; private set; }
        public bool FailWrites { get; set; }
        public CatalogItem? FindCatalogItem(int id) => Items.FirstOrDefault(i => i.Id == id);
        public IReadOnlyList<CatalogBrand> GetCatalogBrands() => [new() { Id = 2, Brand = ".NET" }];
        public IReadOnlyList<CatalogType> GetCatalogTypes() => [new() { Id = 1, Type = "Mug" }];
        public IReadOnlyList<CatalogItem> GetCatalogItems(int brandIdFilter, int typeIdFilter)
        {
            Filters = (brandIdFilter, typeIdFilter);
            return Items;
        }
        public DiscountItem? GetDiscount(DateTime day) => Discount;
        public int GetAvailableStock(DateTime date, int catalogItemId) => 100;
        public void CreateAvailableStock(CatalogItemsStock stock)
        {
            if (FailWrites) throw new InvalidOperationException("write failed");
            Stock = stock;
        }
        public void CreateCatalogItem(CatalogItem item) => throw new NotSupportedException();
        public void UpdateCatalogItem(CatalogItem item) => throw new NotSupportedException();
        public void RemoveCatalogItem(CatalogItem item) => throw new NotSupportedException();
    }

    private sealed class FakeView : ICatalogView
    {
        public event ViewHandler<ICatalogView>? filterChanged;
        public event AvailabilityHandler<ICatalogView>? availabilityButtonClicked;
        public event SearchStockHandler<ICatalogView>? searchStockButtonClicked;
        public CatalogController? Controller { get; private set; }
        public Dictionary<int, string> Brands { get; private set; } = [];
        public Dictionary<int, string> Types { get; private set; } = [];
        public IEnumerable<CatalogItem> Items { get; private set; } = [];
        public IEnumerable<CatalogItem> Shipment { get; private set; } = [];
        public double Discount { get; private set; }
        public string? Banner { get; private set; }
        public int Clears { get; private set; }
        public int Notifications { get; private set; }
        public int Available { get; private set; }
        public SearchStockEventArgs? SearchArgs { get; private set; }
        public void Filter(int brand, int type) => filterChanged?.Invoke(this, new FilterEventArgs(type, brand));
        public void Add(int id, int quantity, DateTime date) => availabilityButtonClicked?.Invoke(this, new AvailabilityEventArgs(id, quantity, date));
        public void Search(int id, DateTime date) => searchStockButtonClicked?.Invoke(this, new SearchStockEventArgs(id, date));
        public void SetController(CatalogController controller) => Controller = controller;
        public void SetCatalogItems(IEnumerable<CatalogItem> items, double discountVal) { Items = items; Discount = discountVal; }
        public void SetShipmentView(IEnumerable<CatalogItem> items) => Shipment = items;
        public void SetDiscountBanner(string text) => Banner = text;
        public void SetBrandFilter(Dictionary<int, string> values) => Brands = values;
        public void SetTypeFilter(Dictionary<int, string> values) => Types = values;
        public void ClearGrid() => Clears++;
        public void NotifyAvailabilityUpdated() => Notifications++;
        public void ShowStockAvailability(SearchStockEventArgs args, int stock) { SearchArgs = args; Available = stock; }
    }
}
