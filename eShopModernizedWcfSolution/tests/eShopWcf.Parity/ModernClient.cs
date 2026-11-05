using System.Globalization;
using System.Text.Json.Nodes;
using eShop.Catalog.Grpc.Protos;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace eShopWcf.Parity;

internal sealed class ModernClient(Catalog.CatalogClient client)
{
    internal async Task<Outcome> CallAsync(string op, JsonObject input)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(25);
        try
        {
            IMessage raw;
            JsonNode? value;
            switch (op)
            {
                case "GetCatalogBrands":
                    var brands = await client.GetCatalogBrandsAsync(new(), deadline: deadline).ResponseAsync.ConfigureAwait(false);
                    raw = brands; value = Canonical.Array(brands.Brands.Select(Canonical.Brand)); break;
                case "GetCatalogTypes":
                    var types = await client.GetCatalogTypesAsync(new(), deadline: deadline).ResponseAsync.ConfigureAwait(false);
                    raw = types; value = Canonical.Array(types.Types_.Select(Canonical.Type)); break;
                case "GetCatalogItems":
                    var items = await client.GetCatalogItemsAsync(new() { BrandIdFilter = Int(input, "brandIdFilter"), TypeIdFilter = Int(input, "typeIdFilter") }, deadline: deadline).ResponseAsync.ConfigureAwait(false);
                    raw = items; value = Canonical.Array(items.Items.Select(Canonical.Item)); break;
                case "FindCatalogItem":
                    var item = await client.FindCatalogItemAsync(new() { Id = Int(input, "id") }, deadline: deadline).ResponseAsync.ConfigureAwait(false);
                    raw = item; value = item.Item is null ? null : Canonical.Item(item.Item); break;
                case "GetDiscount":
                    var discount = await client.GetDiscountAsync(new() { Day = Date(input, "day") }, deadline: deadline).ResponseAsync.ConfigureAwait(false);
                    raw = discount; value = discount.Discount is null ? null : Canonical.Discount(discount.Discount); break;
                case "GetAvailableStock":
                    var stock = await client.GetAvailableStockAsync(new() { Date = Date(input, "date"), CatalogItemId = Int(input, "catalogItemId") }, deadline: deadline).ResponseAsync.ConfigureAwait(false);
                    raw = stock; value = JsonValue.Create(stock.AvailableStock); break;
                case "CreateCatalogItem":
                    raw = await client.CreateCatalogItemAsync(new() { CatalogItem = Item(input) }, deadline: deadline).ResponseAsync.ConfigureAwait(false);
                    value = Canonical.Node(new { ok = true }); break;
                case "UpdateCatalogItem":
                    raw = await client.UpdateCatalogItemAsync(new() { CatalogItem = Item(input) }, deadline: deadline).ResponseAsync.ConfigureAwait(false);
                    value = Canonical.Node(new { ok = true }); break;
                case "RemoveCatalogItem":
                    raw = await client.RemoveCatalogItemAsync(new() { CatalogItem = Item(input) }, deadline: deadline).ResponseAsync.ConfigureAwait(false);
                    value = Canonical.Node(new { ok = true }); break;
                case "CreateAvailableStock":
                    var s = input["stock"]!.AsObject();
                    raw = await client.CreateAvailableStockAsync(new()
                    {
                        CatalogItemsStock = new()
                        {
                            StockId = Int(s, "StockId"),
                            CatalogItemId = Int(s, "CatalogItemId"),
                            AvailableStock = Int(s, "AvailableStock"),
                            Date = Date(s, "Date"),
                        }
                    }, deadline: deadline).ResponseAsync.ConfigureAwait(false);
                    value = Canonical.Node(new { ok = true }); break;
                default: throw new InvalidOperationException("Unsupported gRPC operation " + op);
            }

            return new Outcome(value, JsonFormatter.Default.Format(raw));
        }
        catch (RpcException error)
        {
            return new Outcome(Canonical.Node(new { status = error.StatusCode.ToString() }), error.Status.ToString());
        }
    }

    private static int Int(JsonObject input, string field) => input[field]!.GetValue<int>();
    private static Timestamp Date(JsonObject input, string field) => Timestamp.FromDateTime(
        DateTime.SpecifyKind(DateTime.Parse(input[field]!.GetValue<string>(), CultureInfo.InvariantCulture), DateTimeKind.Utc));

    private static CatalogItem Item(JsonObject input)
    {
        var item = input["item"]!.AsObject();
        decimal price = decimal.Parse(item["Price"]!.GetValue<string>(), CultureInfo.InvariantCulture);
        long units = decimal.ToInt64(decimal.Truncate(price));
        return new CatalogItem
        {
            Id = Int(item, "Id"),
            CatalogBrandId = Int(item, "CatalogBrandId"),
            CatalogTypeId = Int(item, "CatalogTypeId"),
            Description = item["Description"]?.GetValue<string>() ?? "",
            Name = item["Name"]?.GetValue<string>() ?? "",
            PictureFilename = item["Picturefilename"]?.GetValue<string>() ?? "",
            Price = new DecimalValue { Units = units, Nanos = decimal.ToInt32((price - units) * 1_000_000_000M) },
        };
    }
}
