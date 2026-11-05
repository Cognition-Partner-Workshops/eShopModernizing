using eShop.Catalog.Domain;
using eShop.WinForms.Client.Controllers;
using Grpc.Core;
using Xunit;

namespace eShop.Catalog.Grpc.Client.Tests;

public class CatalogControllerTests
{
    [Fact]
    public async Task StartupServiceFailureIsReportedWithoutEscaping()
    {
        var view = new TestView();
        var controller = new CatalogController(new DeferredClient { Unavailable = true }, view);

        await controller.LoadViewAsync();

        Assert.Contains("Unavailable", await view.ErrorReported.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Empty(view.Items);
    }

    [Theory]
    [InlineData("filter")]
    [InlineData("stock")]
    [InlineData("shipment")]
    public async Task EventServiceFailureIsReportedWithoutEscaping(string operation)
    {
        var view = new TestView();
        _ = new CatalogController(new DeferredClient { Unavailable = true }, view);

        view.Raise(operation);

        Assert.Contains("Unavailable", await view.ErrorReported.Task.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OverlappingLoadsOnlyDisplayTheLatestSelection(bool completeLatestFirst)
    {
        var service = new DeferredClient();
        var view = new TestView();
        var controller = new CatalogController(service, view);
        var first = controller.LoadCatalogItemsAsync(1, 0);
        var latest = controller.LoadCatalogItemsAsync(2, 0);

        Assert.Equal(0, view.ClearCount);
        if (completeLatestFirst)
        {
            service.Latest.SetResult([new() { Id = 2 }]);
            await latest;
            service.First.SetResult([new() { Id = 1 }]);
            await first;
        }
        else
        {
            service.First.SetResult([new() { Id = 1 }]);
            await first;
            service.Latest.SetResult([new() { Id = 2 }]);
            await latest;
        }

        Assert.Equal(1, view.ClearCount);
        Assert.Equal(2, Assert.Single(view.Items).Id);
    }

    private sealed class DeferredClient : ICatalogServiceClient
    {
        public bool Unavailable { get; init; }
        private static RpcException ServiceUnavailable() => new(new Status(StatusCode.Unavailable, "Test backend unavailable"));
        public TaskCompletionSource<IReadOnlyList<CatalogItem>> First { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<IReadOnlyList<CatalogItem>> Latest { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<IReadOnlyList<CatalogItem>> GetCatalogItemsAsync(int brandIdFilter, int typeIdFilter, CancellationToken cancellationToken = default) => Unavailable ? Task.FromException<IReadOnlyList<CatalogItem>>(ServiceUnavailable()) : brandIdFilter == 1 ? First.Task : Latest.Task;
        public Task<DiscountItem?> GetDiscountAsync(DateTime day, CancellationToken cancellationToken = default) => Unavailable ? Task.FromException<DiscountItem?>(ServiceUnavailable()) : Task.FromResult<DiscountItem?>(null);
        public Task<CatalogItem?> FindCatalogItemAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<CatalogBrand>> GetCatalogBrandsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<CatalogType>> GetCatalogTypesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> GetAvailableStockAsync(DateTime date, int catalogItemId, CancellationToken cancellationToken = default) => Task.FromException<int>(ServiceUnavailable());
        public Task CreateAvailableStockAsync(CatalogItemsStock stock, CancellationToken cancellationToken = default) => Task.FromException(ServiceUnavailable());
        public Task CreateCatalogItemAsync(CatalogItem item, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateCatalogItemAsync(CatalogItem item, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RemoveCatalogItemAsync(CatalogItem item, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestView : ICatalogView
    {
        public event ViewHandler<ICatalogView>? FilterChanged;
        public event AvailabilityHandler<ICatalogView>? AvailabilityButtonClicked;
        public event SearchStockHandler<ICatalogView>? SearchStockButtonClicked;
        public TaskCompletionSource<string> ErrorReported { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void ShowServiceError(string message) => ErrorReported.TrySetResult(message);
        public void Raise(string operation)
        {
            switch (operation)
            {
                case "filter": FilterChanged?.Invoke(this, new FilterEventArgs(0, 0)); break;
                case "stock": SearchStockButtonClicked?.Invoke(this, new SearchStockEventArgs(1, DateTime.Today)); break;
                case "shipment": AvailabilityButtonClicked?.Invoke(this, new AvailabilityEventArgs(1, 10, DateTime.Today)); break;
            }
        }
        public List<CatalogItem> Items { get; } = [];
        public int ClearCount { get; private set; }
        public void ClearGrid() { ClearCount++; Items.Clear(); }
        public void SetCatalogItems(IEnumerable<CatalogItem> items, double discountVal) => Items.AddRange(items);
        public void SetController(CatalogController controller) { }
        public void SetDiscountBanner(string bannerText) { }
        public void SetTypeFilter(Dictionary<int, string> typeFilters) { }
        public void SetBrandFilter(Dictionary<int, string> brandFilter) { }
        public void NotifyAvailabilityUpdated() { }
        public void ShowStockAvailability(SearchStockEventArgs args, int stock) { }
        public void SetShipmentView(IEnumerable<CatalogItem> items) { }
    }
}
