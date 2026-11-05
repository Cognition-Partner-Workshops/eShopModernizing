using eShop.Catalog.Domain;
using eShop.Catalog.Grpc.Client;

namespace eShop.WinForms.Client.Controllers;

/// <summary>
/// Presenter for <see cref="ICatalogView"/>. Same responsibilities as the legacy controller; the
/// only change is that the service it drives is the gRPC client wrapper instead of the generated
/// WCF proxy, so every call is asynchronous.
/// </summary>
public class CatalogController
{
    private readonly ICatalogServiceClient _service;
    private readonly ICatalogView _view;

    public CatalogController(ICatalogServiceClient service, ICatalogView view)
    {
        _service = service;
        _view = view;
        _view.FilterChanged += OnFilterChanged;
        _view.AvailabilityButtonClicked += OnAvailabilityRequested;
        _view.SearchStockButtonClicked += OnStockSearchRequested;
    }

    /// <summary>Initial load: discount banner, catalog grid, both filters and the shipment tab.</summary>
    public async Task LoadViewAsync(CancellationToken cancellationToken = default)
    {
        _view.SetController(this);
        await CheckForDiscountsAsync(cancellationToken);
        await LoadCatalogItemsAsync(0, 0, cancellationToken);
        await LoadBrandFiltersAsync(cancellationToken);
        await LoadTypeFiltersAsync(cancellationToken);
        await SetShipmentViewAsync(cancellationToken);
    }

    public async Task LoadCatalogItemsAsync(int brandIdFilter, int typeIdFilter, CancellationToken cancellationToken = default)
    {
        _view.ClearGrid();

        var items = await _service.GetCatalogItemsAsync(brandIdFilter, typeIdFilter, cancellationToken);
        var discount = await _service.GetDiscountAsync(DateTime.Now, cancellationToken);

        _view.SetCatalogItems(items, discount?.Size ?? 0);
    }

    public async Task AddAvailabilityAsync(AvailabilityEventArgs e, CancellationToken cancellationToken = default)
    {
        var shipment = new CatalogItemsStock
        {
            CatalogItemId = e.ItemId,
            AvailableStock = e.ItemStock,
            Date = e.ShipDate,
        };

        await _service.CreateAvailableStockAsync(shipment, cancellationToken);
        _view.NotifyAvailabilityUpdated();
    }

    public async Task SearchStockAvailableAsync(SearchStockEventArgs e, CancellationToken cancellationToken = default)
    {
        var stock = await _service.GetAvailableStockAsync(e.Date, e.ItemId, cancellationToken);

        _view.ShowStockAvailability(e, stock);
    }

    private async Task CheckForDiscountsAsync(CancellationToken cancellationToken)
    {
        var discount = await _service.GetDiscountAsync(DateTime.Now, cancellationToken);
        if (discount is null)
        {
            return;
        }

        var discountPercentage = Math.Round(discount.Size * 100, 0);

        _view.SetDiscountBanner($"{discountPercentage}% sale endson {discount.End.ToShortDateString()}!");
    }

    private async Task SetShipmentViewAsync(CancellationToken cancellationToken)
    {
        var items = await _service.GetCatalogItemsAsync(0, 0, cancellationToken);

        _view.SetShipmentView(items);
    }

    private async Task LoadBrandFiltersAsync(CancellationToken cancellationToken)
    {
        var brands = await _service.GetCatalogBrandsAsync(cancellationToken);

        // The service does not return an "all" entry, so the view adds it.
        var brandDictionary = new Dictionary<int, string> { [0] = "All" };
        foreach (var brand in brands)
        {
            brandDictionary[brand.Id] = brand.Brand;
        }

        _view.SetBrandFilter(brandDictionary);
    }

    private async Task LoadTypeFiltersAsync(CancellationToken cancellationToken)
    {
        var types = await _service.GetCatalogTypesAsync(cancellationToken);

        var typeDictionary = new Dictionary<int, string> { [0] = "All" };
        foreach (var type in types)
        {
            typeDictionary[type.Id] = type.Type;
        }

        _view.SetTypeFilter(typeDictionary);
    }

    private async void OnFilterChanged(ICatalogView view, FilterEventArgs e) =>
        await LoadCatalogItemsAsync(e.BrandFilterValue, e.TypeFilterValue);

    private async void OnAvailabilityRequested(ICatalogView view, AvailabilityEventArgs e) =>
        await AddAvailabilityAsync(e);

    private async void OnStockSearchRequested(ICatalogView view, SearchStockEventArgs e) =>
        await SearchStockAvailableAsync(e);
}
