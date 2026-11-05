using eShop.Catalog.Domain;
using eShop.WinForms.Client.Controllers;
using Xunit;

namespace eShop.Catalog.Grpc.Client.Tests;

public class CatalogControllerTests
{
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
        public TaskCompletionSource<IReadOnlyList<CatalogItem>> First { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<IReadOnlyList<CatalogItem>> Latest { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<IReadOnlyList<CatalogItem>> GetCatalogItemsAsync(int brandIdFilter, int typeIdFilter, CancellationToken cancellationToken = default) => brandIdFilter == 1 ? First.Task : Latest.Task;
        public Task<DiscountItem?> GetDiscountAsync(DateTime day, CancellationToken cancellationToken = default) => Task.FromResult<DiscountItem?>(null);
        public Task<CatalogItem?> FindCatalogItemAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<CatalogBrand>> GetCatalogBrandsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<CatalogType>> GetCatalogTypesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> GetAvailableStockAsync(DateTime date, int catalogItemId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CreateAvailableStockAsync(CatalogItemsStock stock, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CreateCatalogItemAsync(CatalogItem item, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateCatalogItemAsync(CatalogItem item, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RemoveCatalogItemAsync(CatalogItem item, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestView : ICatalogView
    {
        public event ViewHandler<ICatalogView>? FilterChanged { add { } remove { } }
        public event AvailabilityHandler<ICatalogView>? AvailabilityButtonClicked { add { } remove { } }
        public event SearchStockHandler<ICatalogView>? SearchStockButtonClicked { add { } remove { } }
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
