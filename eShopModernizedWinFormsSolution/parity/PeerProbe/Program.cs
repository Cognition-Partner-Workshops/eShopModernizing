using System.Globalization;
using System.Text.Json;
using eShopWinForms.Grpc.Client;
using eShopWinForms.Grpc.Client.Models;

if (args.Length != 3 || args[2] != "--allow-writes")
{
    Console.Error.WriteLine("Usage: PeerProbe <server-url> <output-json> --allow-writes (disposable fixture database only)");
    return 2;
}

using var client = new CatalogServiceGrpcClient(new CatalogServiceClientOptions { Address = args[0] });
var results = new SortedDictionary<string, object>(StringComparer.Ordinal);
var failed = 0;
void Check(string name, Func<object> run)
{
    try { results[name] = new { passed = true, value = run() }; }
    catch (Exception error)
    {
        failed++;
        results[name] = new { passed = false, error = error.ToString() };
    }
}
static object Require(bool valid, object value) => valid ? value : throw new InvalidOperationException(JsonSerializer.Serialize(value));

var brands = client.GetCatalogBrands();
var types = client.GetCatalogTypes();
var items = client.GetCatalogItems(0, 0);
Check("GetCatalogBrands", () => Require(brands.Count > 0, brands));
Check("GetCatalogTypes", () => Require(types.Count > 0, types));
Check("GetCatalogItems", () => Require(items.Count > 0, items));
Check("FindCatalogItem", () => Require(client.FindCatalogItem(items[0].Id)?.Price == items[0].Price, client.FindCatalogItem(items[0].Id)!));
Check("FindCatalogItem.missing", () => Require(client.FindCatalogItem(int.MaxValue) is null, "NOT_FOUND maps to null"));
Check("filters", () =>
{
    var filtered = client.GetCatalogItems(items[0].CatalogBrandId, items[0].CatalogTypeId);
    return Require(filtered.All(x => x.CatalogBrandId == items[0].CatalogBrandId && x.CatalogTypeId == items[0].CatalogTypeId), filtered);
});
Check("GetAvailableStock.missing", () => Require(client.GetAvailableStock(new DateTime(2038, 1, 1), items[0].Id) == 0, "missing stock returns zero"));
Check("GetDiscount.boundaries", () =>
{
    var first = client.GetDiscount(new DateTime(2017, 9, 18, 23, 59, 59, DateTimeKind.Local));
    var last = client.GetDiscount(new DateTime(2017, 9, 21, 23, 59, 59, DateTimeKind.Unspecified));
    var after = client.GetDiscount(new DateTime(2017, 9, 22));
    return Require(first?.Id == 1 && last?.Id == 1 && after?.Id == 2, new { first, last, after });
});
Check("GetDiscount.missing", () => Require(client.GetDiscount(new DateTime(2038, 1, 1)) is null, "NOT_FOUND maps to null"));

Check("CreateAvailableStock.dateOnly", () =>
{
    var date = new DateTime(2037, 1, 15, 23, 59, 59, DateTimeKind.Local);
    client.CreateAvailableStock(new CatalogItemsStock { CatalogItemId = items[0].Id, Date = date, AvailableStock = 53 });
    var first = client.GetAvailableStock(date.Date, items[0].Id);
    client.CreateAvailableStock(new CatalogItemsStock { CatalogItemId = items[0].Id, Date = date.Date, AvailableStock = 75 });
    var second = client.GetAvailableStock(date, items[0].Id);
    return Require(first == 53 && second == 75, new { first, second, date = date.ToString("O", CultureInfo.InvariantCulture), date.Kind });
});

var marker = "PeerProbe-" + Guid.NewGuid().ToString("N");
var id = 0;
Check("CreateCatalogItem.exactDecimal", () =>
{
    client.CreateCatalogItem(new CatalogItem
    {
        Name = marker,
        Description = null,
        Picturefilename = "1.png",
        Price = 12.3456M,
        CatalogBrandId = brands[0].Id,
        CatalogTypeId = types[0].Id
    });
    var created = client.GetCatalogItems(0, 0).Single(x => x.Name == marker);
    id = created.Id;
    return Require(created.Price == 12.3456M && created.Description == string.Empty, created);
});
Check("UpdateCatalogItem.exactDecimal", () =>
{
    var created = client.FindCatalogItem(id) ?? throw new InvalidOperationException("Created row missing");
    created.Price = -12.3456M;
    created.Description = "Updated";
    client.UpdateCatalogItem(created);
    var updated = client.FindCatalogItem(id)!;
    return Require(updated.Price == -12.3456M && updated.Description == "Updated", updated);
});
Check("RemoveCatalogItem", () =>
{
    var created = client.FindCatalogItem(id) ?? throw new InvalidOperationException("Created row missing");
    client.RemoveCatalogItem(created);
    return Require(client.FindCatalogItem(id) is null, "Removed row maps to null");
});
File.WriteAllText(args[1], JsonSerializer.Serialize(new { endpoint = args[0], failed, results }, new JsonSerializerOptions { WriteIndented = true }) + "\n");
Console.WriteLine($"{results.Count - failed} passed, {failed} failed; all ten RPCs exercised via the desktop client wrapper");
return failed == 0 ? 0 : 1;
