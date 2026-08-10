using eShop.Catalog.Domain;

namespace eShop.WinForms.Client.Controllers;

public delegate void ViewHandler<TView>(TView sender, FilterEventArgs e);
public delegate void AvailabilityHandler<TView>(TView sender, AvailabilityEventArgs e);
public delegate void SearchStockHandler<TView>(TView sender, SearchStockEventArgs e);

/// <summary>Brand/type filter selection, as in the legacy client.</summary>
public class FilterEventArgs : EventArgs
{
    public FilterEventArgs(int typeId, int brandId)
    {
        TypeFilterValue = typeId;
        BrandFilterValue = brandId;
    }

    public int TypeFilterValue { get; }

    public int BrandFilterValue { get; }
}

/// <summary>A new shipment to record for an item on a given day.</summary>
public class AvailabilityEventArgs : EventArgs
{
    public AvailabilityEventArgs(int id, int stock, DateTime ship)
    {
        ItemId = id;
        ItemStock = stock;
        ShipDate = ship;
    }

    public int ItemId { get; }

    public int ItemStock { get; }

    public DateTime ShipDate { get; }
}

/// <summary>A stock availability lookup for an item on a given day.</summary>
public class SearchStockEventArgs : EventArgs
{
    public SearchStockEventArgs(int id, DateTime date)
    {
        ItemId = id;
        Date = date;
    }

    public int ItemId { get; }

    public DateTime Date { get; }
}

public interface ICatalogView
{
    event ViewHandler<ICatalogView>? FilterChanged;

    event AvailabilityHandler<ICatalogView>? AvailabilityButtonClicked;

    event SearchStockHandler<ICatalogView>? SearchStockButtonClicked;

    void SetController(CatalogController controller);

    void SetCatalogItems(IEnumerable<CatalogItem> items, double discountVal);

    void SetDiscountBanner(string bannerText);

    void SetTypeFilter(Dictionary<int, string> typeFilters);

    void SetBrandFilter(Dictionary<int, string> brandFilter);

    void ClearGrid();

    void NotifyAvailabilityUpdated();

    void ShowStockAvailability(SearchStockEventArgs args, int stock);

    void SetShipmentView(IEnumerable<CatalogItem> items);
}
