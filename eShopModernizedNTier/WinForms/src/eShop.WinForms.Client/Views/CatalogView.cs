using System.ComponentModel;
using eShop.Catalog.Domain;
using eShop.WinForms.Client.Controllers;
using eShop.WinForms.Client.Infrastructure;

namespace eShop.WinForms.Client.Views;

public partial class CatalogView : Form, ICatalogView
{
    private CatalogController? _controller;

    public CatalogView()
    {
        InitializeComponent();
        listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
    }

    public event ViewHandler<ICatalogView>? FilterChanged;

    public event SearchStockHandler<ICatalogView>? SearchStockButtonClicked;

    public event AvailabilityHandler<ICatalogView>? AvailabilityButtonClicked;

    public void SetController(CatalogController controller) => _controller = controller;

    public void ClearGrid()
    {
        catalogItemDataGridView.Rows.Clear();
        catalogItemDataGridView.Refresh();
    }

    /// <summary>Fills the grid, applying the running discount to each price.</summary>
    public void SetCatalogItems(IEnumerable<CatalogItem> items, double discountVal)
    {
        foreach (var catalogItem in items)
        {
            var price = (double)catalogItem.Price;
            var discountPrice = price * (1 - discountVal);

            using var picture = AssetImages.LoadCatalogPicture(catalogItem.PictureFileName);
            var thumb = picture?.GetThumbnailImage(384, 216, null, IntPtr.Zero);

            catalogItemDataGridView.Rows.Add(
                thumb,
                catalogItem.Id.ToString(),
                catalogItem.Name,
                catalogItem.Description,
                string.Concat("$", discountPrice.ToString("F")));
        }
    }

    public void SetShipmentView(IEnumerable<CatalogItem> items)
    {
        foreach (var catalogItem in items)
        {
            listBox1.Items.Add($"{catalogItem.Id} - {catalogItem.Name}");
            productIdInput.Items.Add(catalogItem.Id);
        }
    }

    public void SetDiscountBanner(string text) => discountBanner.Text = text;

    public void SetTypeFilter(Dictionary<int, string> typeFilters)
    {
        catalogTypeComboBox.DataSource = new BindingSource(typeFilters, null);
        catalogTypeComboBox.DisplayMember = "Value";
        catalogTypeComboBox.ValueMember = "Key";
    }

    public void SetBrandFilter(Dictionary<int, string> brandFilter)
    {
        catalogBrandComboBox.DataSource = new BindingSource(brandFilter, null);
        catalogBrandComboBox.DisplayMember = "Value";
        catalogBrandComboBox.ValueMember = "Key";
    }

    /// <summary>Latest result of a stock availability lookup.</summary>
    public void ShowStockAvailability(SearchStockEventArgs args, int availability)
    {
        var item = new ListViewItem(args.Date.ToShortDateString());
        item.SubItems.Add(args.ItemId.ToString());
        item.SubItems.Add(availability.ToString());
        listView1.Items.Add(item);

        listView1.Columns[0].Width = -1;
        listView1.Columns[1].Width = -2;
        listView1.Columns[2].Width = -2;
    }

    public void NotifyAvailabilityUpdated()
    {
        MessageBox.Show("Shipment has been added to the database.");

        productIdInput.ResetText();
        quantityInput.Clear();
        arrivalDateInput.Clear();
    }

    private void catalogBrandComboBox_SelectedIndexChanged(object sender, EventArgs e) => RaiseFilterChanged();

    private void catalogTypeComboBox_SelectedIndexChanged(object sender, EventArgs e) => RaiseFilterChanged();

    private void RaiseFilterChanged()
    {
        var brandId = catalogBrandComboBox.SelectedItem is KeyValuePair<int, string> brand ? brand.Key : 0;
        var typeId = catalogTypeComboBox.SelectedItem is KeyValuePair<int, string> type ? type.Key : 0;

        FilterChanged?.Invoke(this, new FilterEventArgs(typeId, brandId));
    }

    private void searchAvailabilityButton_Click(object sender, EventArgs e)
    {
        if (listBox1.SelectedItem is null)
        {
            return;
        }

        var results = listBox1.SelectedItem.ToString()!.Split([" - "], StringSplitOptions.None);
        var date = monthCalendar1.SelectionRange.Start.Date;
        var id = int.Parse(results[0]);

        SearchStockButtonClicked?.Invoke(this, new SearchStockEventArgs(id, date));
    }

    private void addAvailabilityButton_Click(object sender, EventArgs e)
    {
        if (productIdInput.SelectedItem is null
            || string.IsNullOrEmpty(quantityInput.Text)
            || string.IsNullOrEmpty(arrivalDateInput.Text))
        {
            return;
        }

        var id = (int)productIdInput.SelectedItem;
        var quantity = int.Parse(quantityInput.Text);
        var shipDate = Convert.ToDateTime(arrivalDateInput.Text);

        AvailabilityButtonClicked?.Invoke(this, new AvailabilityEventArgs(id, quantity, shipDate));
    }
}
