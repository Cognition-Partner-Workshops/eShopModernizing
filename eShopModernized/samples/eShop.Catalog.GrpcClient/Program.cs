using System.Globalization;
using eShop.Catalog.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;

namespace eShop.Catalog.GrpcClient;

/// <summary>
/// Cross-platform console client for the modernized catalog gRPC service. It replaces the retired
/// WinForms desktop client (decision D-07): <c>demo</c> reproduces that client's workflow, and the
/// remaining commands cover the four operations the WinForms client never called.
/// </summary>
internal static class Program
{
    private const string DefaultAddress = "http://localhost:5095";

    public static async Task<int> Main(string[] args)
    {
        var arguments = new List<string>(args);
        var address = TakeOption(arguments, "--address") ?? DefaultAddress;

        if (arguments.Count == 0 || arguments[0] is "-h" or "--help" or "help")
        {
            PrintUsage();
            return arguments.Count == 0 ? 1 : 0;
        }

        using var channel = GrpcChannel.ForAddress(address);
        var client = new CatalogService.CatalogServiceClient(channel);

        try
        {
            return await ExecuteAsync(client, arguments).ConfigureAwait(false);
        }
        catch (RpcException exception)
        {
            await Console.Error.WriteLineAsync(
                $"{exception.StatusCode}: {exception.Status.Detail}").ConfigureAwait(false);
            return 1;
        }
    }

    private static async Task<int> ExecuteAsync(CatalogService.CatalogServiceClient client, List<string> arguments)
    {
        switch (arguments[0])
        {
            case "brands":
                await PrintBrandsAsync(client).ConfigureAwait(false);
                return 0;

            case "types":
                await PrintTypesAsync(client).ConfigureAwait(false);
                return 0;

            case "items":
                await PrintItemsAsync(
                    client,
                    ArgumentAt(arguments, 1) is { } brand ? ParseInt(brand, "brandIdFilter") : 0,
                    ArgumentAt(arguments, 2) is { } type ? ParseInt(type, "typeIdFilter") : 0)
                    .ConfigureAwait(false);
                return 0;

            case "find":
                {
                    var item = await client.FindCatalogItemAsync(
                        new FindCatalogItemRequest { Id = ParseInt(RequiredArgument(arguments, 1, "id"), "id") });
                    Console.WriteLine(Describe(item));
                    return 0;
                }

            case "create":
                await client.CreateCatalogItemAsync(ReadItem(arguments, idIndex: null)).ConfigureAwait(false);
                Console.WriteLine("Created.");
                return 0;

            case "update":
                await client.UpdateCatalogItemAsync(ReadItem(arguments, idIndex: 1)).ConfigureAwait(false);
                Console.WriteLine("Updated.");
                return 0;

            case "remove":
                await client.RemoveCatalogItemAsync(
                    new CatalogItem { Id = ParseInt(RequiredArgument(arguments, 1, "id"), "id") }).ConfigureAwait(false);
                Console.WriteLine("Removed.");
                return 0;

            case "stock":
                {
                    var response = await client.GetAvailableStockAsync(new GetAvailableStockRequest
                    {
                        CatalogItemId = ParseInt(RequiredArgument(arguments, 1, "catalogItemId"), "catalogItemId"),
                        Date = ParseDate(RequiredArgument(arguments, 2, "date")),
                    });
                    Console.WriteLine(response.AvailableStock);
                    return 0;
                }

            case "add-stock":
                await client.CreateAvailableStockAsync(new CatalogItemsStock
                {
                    CatalogItemId = ParseInt(RequiredArgument(arguments, 1, "catalogItemId"), "catalogItemId"),
                    Date = ParseDate(RequiredArgument(arguments, 2, "date")),
                    AvailableStock = ParseInt(RequiredArgument(arguments, 3, "quantity"), "quantity"),
                }).ConfigureAwait(false);
                Console.WriteLine("Stock recorded.");
                return 0;

            case "discount":
                {
                    var day = ArgumentAt(arguments, 1) is { } value
                        ? ParseDate(value)
                        : Timestamp.FromDateTime(DateTime.UtcNow.Date);
                    var discount = await client.GetDiscountAsync(new GetDiscountRequest { Day = day });
                    Console.WriteLine(
                        FormattableString.Invariant(
                            $"{Math.Round(discount.Size * 100, 0)}% sale ends on {discount.End.ToDateTime():yyyy-MM-dd}"));
                    return 0;
                }

            case "demo":
                await RunWinFormsWorkflowAsync(client).ConfigureAwait(false);
                return 0;

            default:
                await Console.Error.WriteLineAsync($"Unknown command '{arguments[0]}'.").ConfigureAwait(false);
                PrintUsage();
                return 1;
        }
    }

    /// <summary>
    /// The workflow the retired WinForms client performed on start-up and on every filter change:
    /// load the brand and type filters, list the filtered catalog, look up today's discount, record
    /// a shipment and read the resulting availability back.
    /// </summary>
    private static async Task RunWinFormsWorkflowAsync(CatalogService.CatalogServiceClient client)
    {
        await PrintBrandsAsync(client).ConfigureAwait(false);
        await PrintTypesAsync(client).ConfigureAwait(false);
        await PrintItemsAsync(client, brandIdFilter: 0, typeIdFilter: 0).ConfigureAwait(false);

        var today = Timestamp.FromDateTime(DateTime.UtcNow.Date);

        try
        {
            var discount = await client.GetDiscountAsync(new GetDiscountRequest { Day = today });
            Console.WriteLine(
                FormattableString.Invariant($"Discount: {Math.Round(discount.Size * 100, 0)}%"));
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
        {
            // The legacy service answered with a null DiscountItem here; the WinForms client
            // null-checked it and left the banner empty.
            Console.WriteLine("Discount: none today");
        }

        var items = await client.GetCatalogItemsAsync(new GetCatalogItemsRequest());
        if (items.Items.Count == 0)
        {
            return;
        }

        var itemId = items.Items[0].Id;

        await client.CreateAvailableStockAsync(new CatalogItemsStock
        {
            CatalogItemId = itemId,
            AvailableStock = 25,
            Date = today,
        }).ConfigureAwait(false);

        var stock = await client.GetAvailableStockAsync(new GetAvailableStockRequest
        {
            CatalogItemId = itemId,
            Date = today,
        });

        Console.WriteLine(
            FormattableString.Invariant($"Available stock for item {itemId} today: {stock.AvailableStock}"));
    }

    private static async Task PrintBrandsAsync(CatalogService.CatalogServiceClient client)
    {
        var response = await client.GetCatalogBrandsAsync(new Empty());

        Console.WriteLine("Brands:");
        foreach (var brand in response.Brands)
        {
            Console.WriteLine(FormattableString.Invariant($"  {brand.Id,3}  {brand.Brand}"));
        }
    }

    private static async Task PrintTypesAsync(CatalogService.CatalogServiceClient client)
    {
        var response = await client.GetCatalogTypesAsync(new Empty());

        Console.WriteLine("Types:");
        foreach (var type in response.CatalogTypes)
        {
            Console.WriteLine(FormattableString.Invariant($"  {type.Id,3}  {type.Type}"));
        }
    }

    private static async Task PrintItemsAsync(
        CatalogService.CatalogServiceClient client,
        int brandIdFilter,
        int typeIdFilter)
    {
        var response = await client.GetCatalogItemsAsync(new GetCatalogItemsRequest
        {
            BrandIdFilter = brandIdFilter,
            TypeIdFilter = typeIdFilter,
        });

        Console.WriteLine(FormattableString.Invariant($"Items ({response.Items.Count}):"));
        foreach (var item in response.Items)
        {
            Console.WriteLine("  " + Describe(item));
        }
    }

    private static string Describe(CatalogItem item)
        => FormattableString.Invariant(
            $"{item.Id,3}  {item.Name,-28}  {item.Price.Value,10}  brand={item.CatalogBrandId} type={item.CatalogTypeId}  {item.PictureFileName}");

    private static CatalogItem ReadItem(List<string> arguments, int? idIndex)
    {
        var offset = idIndex.HasValue ? idIndex.Value + 1 : 1;

        var item = new CatalogItem
        {
            Name = RequiredArgument(arguments, offset, "name"),
            Price = new DecimalValue { Value = ParseDecimal(RequiredArgument(arguments, offset + 1, "price")) },
            CatalogBrandId = ParseInt(RequiredArgument(arguments, offset + 2, "brandId"), "brandId"),
            CatalogTypeId = ParseInt(RequiredArgument(arguments, offset + 3, "typeId"), "typeId"),
            Description = ArgumentAt(arguments, offset + 4) ?? string.Empty,
            PictureFileName = ArgumentAt(arguments, offset + 5) ?? string.Empty,
        };

        if (idIndex.HasValue)
        {
            item.Id = ParseInt(RequiredArgument(arguments, idIndex.Value, "id"), "id");
        }

        return item;
    }

    private static string? TakeOption(List<string> arguments, string name)
    {
        var index = arguments.IndexOf(name);
        if (index < 0 || index + 1 >= arguments.Count)
        {
            return null;
        }

        var value = arguments[index + 1];
        arguments.RemoveRange(index, 2);
        return value;
    }

    private static string? ArgumentAt(List<string> arguments, int index)
        => index < arguments.Count ? arguments[index] : null;

    private static string RequiredArgument(List<string> arguments, int index, string name)
        => ArgumentAt(arguments, index) ?? throw new ArgumentException($"Missing argument '{name}'.", nameof(arguments));

    private static int ParseInt(string value, string name)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new ArgumentException($"'{name}' must be an integer, but was '{value}'.", nameof(value));

    /// <summary>Normalizes to the invariant culture, which is what DecimalValue carries.</summary>
    private static string ParseDecimal(string value)
        => decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed)
            ? parsed.ToString(CultureInfo.InvariantCulture)
            : throw new ArgumentException($"'price' must be an invariant-culture decimal, but was '{value}'.", nameof(value));

    private static Timestamp ParseDate(string value)
        => DateTime.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed)
            ? Timestamp.FromDateTime(DateTime.SpecifyKind(parsed, DateTimeKind.Utc))
            : throw new ArgumentException($"Dates must be yyyy-MM-dd, but was '{value}'.", nameof(value));

    private static void PrintUsage()
        => Console.WriteLine(
            """
            eShop catalog gRPC sample client.

            Usage: eShop.Catalog.GrpcClient [--address <url>] <command> [arguments]
                   (default address: http://localhost:5095)

            Commands (one per operation of the legacy WCF ICatalogService):
              brands                                                     GetCatalogBrands
              types                                                      GetCatalogTypes
              items [brandIdFilter] [typeIdFilter]                       GetCatalogItems (0 = no filter)
              find <id>                                                  FindCatalogItem
              create <name> <price> <brandId> <typeId> [desc] [picture]  CreateCatalogItem
              update <id> <name> <price> <brandId> <typeId> [desc] [pic] UpdateCatalogItem
              remove <id>                                                RemoveCatalogItem
              stock <catalogItemId> <yyyy-MM-dd>                         GetAvailableStock
              add-stock <catalogItemId> <yyyy-MM-dd> <quantity>          CreateAvailableStock
              discount [yyyy-MM-dd]                                      GetDiscount
              demo                                                       the retired WinForms client's workflow
            """);
}
