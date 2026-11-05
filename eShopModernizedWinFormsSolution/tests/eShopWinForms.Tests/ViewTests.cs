using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using eShopWinForms.Grpc.Client.Models;
using Xunit;

namespace eShopWinForms.Tests;

public sealed class ViewTests
{
    [Theory]
    [InlineData("19.5", ".15", "$16.58")]
    [InlineData("8.5", ".15", "$7.23")]
    [InlineData("12", ".15", "$10.20")]
    [InlineData("19.5", "0", "$19.50")]
    [InlineData("0", ".15", "$0.00")]
    [InlineData("1.005", "0", "$1.01")]
    [InlineData("-19.5", ".15", "$-16.58")]
    public void PricesRetainFrameworkRoundingAndTwoDecimalPlaces(string price, string discount, string expected)
    {
        OnSta(() =>
        {
            using var view = new CatalogView();
            view.SetCatalogItems([new CatalogItem
            {
                Id = 1, Name = "Item", Description = "Item", Picturefilename = "1.png",
                Price = decimal.Parse(price, CultureInfo.InvariantCulture)
            }], double.Parse(discount, CultureInfo.InvariantCulture));
            var grid = Grid(view);
            Assert.Equal(expected, grid.Rows[0].Cells[4].Value);
            view.ClearGrid();
            view.SetCatalogItems([], .15);
            Assert.DoesNotContain(grid.Rows.Cast<DataGridViewRow>(), row => !row.IsNewRow);
        });
    }

    private static DataGridView Grid(CatalogView view) => (DataGridView)typeof(CatalogView)
        .GetField("catalogItemDataGridView", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

    private static void OnSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            try { action(); }
            catch (Exception exception) { error = exception; }
        })
        { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "STA view test timed out.");
        if (error is not null) ExceptionDispatchInfo.Capture(error).Throw();
    }
}
