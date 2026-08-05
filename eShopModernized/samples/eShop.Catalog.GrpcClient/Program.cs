using eShop.Catalog.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;

namespace eShop.Catalog.GrpcClient;

/// <summary>
/// Cross-platform console client for the modernized catalog gRPC service. It is the replacement
/// for the retired WinForms desktop client (decision D-07): the <c>catalog</c> and
/// <c>inventory</c> verbs reproduce that client's two tabs, and the remaining verbs expose the
/// individual operations of the legacy WCF <c>ICatalogService</c> one by one.
/// </summary>
internal static class Program
{
    private const int UsageExitCode = 2;

    public static async Task<int> Main(string[] args)
    {
        CommandLine commandLine;

        try
        {
            commandLine = CommandLine.Parse(args);
        }
        catch (CommandLineException exception)
        {
            return await UsageErrorAsync(exception.Message).ConfigureAwait(false);
        }

        if (commandLine.Verb == CommandLine.HelpVerb)
        {
            Console.Write(CommandCatalog.UsageText);
            return commandLine.IsEmpty ? UsageExitCode : 0;
        }

        if (!CommandCatalog.IsKnown(commandLine.Verb))
        {
            return await UsageErrorAsync($"Unknown command '{commandLine.Verb}'.").ConfigureAwait(false);
        }

        using var channel = GrpcChannel.ForAddress(commandLine.Address);
        var client = new CatalogService.CatalogServiceClient(channel);

        try
        {
            return await ExecuteAsync(client, commandLine).ConfigureAwait(false);
        }
        catch (CommandLineException exception)
        {
            return await UsageErrorAsync(exception.Message).ConfigureAwait(false);
        }
        catch (RpcException exception)
        {
            await Console.Error.WriteLineAsync($"{exception.StatusCode}: {exception.Status.Detail}").ConfigureAwait(false);
            return 1;
        }
    }

    private static async Task<int> ExecuteAsync(CatalogService.CatalogServiceClient client, CommandLine commandLine)
    {
        switch (commandLine.Verb)
        {
            case "get-brands":
                await PrintBrandsAsync(client).ConfigureAwait(false);
                return 0;

            case "get-types":
                await PrintTypesAsync(client).ConfigureAwait(false);
                return 0;

            case "get-items":
                await PrintItemsAsync(client, ReadFilters(commandLine), discount: null).ConfigureAwait(false);
                return 0;

            case "find-item":
                {
                    var item = await client.FindCatalogItemAsync(new FindCatalogItemRequest
                    {
                        Id = ValueParsing.ParsePositiveInt(commandLine.Required(0, "id"), "id"),
                    });

                    Console.WriteLine(CatalogFormatting.FormatItem(item));
                    return 0;
                }

            case "get-stock":
                {
                    var itemId = ValueParsing.ParsePositiveInt(commandLine.Required(0, "itemId"), "itemId");
                    var date = ValueParsing.ParseDate(commandLine.Required(1, "date"), "date");

                    await PrintStockAsync(client, itemId, date).ConfigureAwait(false);
                    return 0;
                }

            case "create-stock":
                {
                    var itemId = ValueParsing.ParsePositiveInt(commandLine.Required(0, "itemId"), "itemId");
                    var date = ValueParsing.ParseDate(commandLine.Required(1, "date"), "date");
                    var quantity = ValueParsing.ParseNonNegativeInt(commandLine.Required(2, "quantity"), "quantity");

                    await CreateStockAsync(client, itemId, date, quantity).ConfigureAwait(false);
                    return 0;
                }

            case "get-discount":
                {
                    var day = ValueParsing.ParseDate(commandLine.Optional(0) ?? ValueParsing.TodayKeyword, "day");
                    var discount = await FindDiscountAsync(client, day).ConfigureAwait(false);

                    Console.WriteLine(discount is null
                        ? CatalogFormatting.NoDiscountMessage
                        : CatalogFormatting.FormatDiscountBanner(discount.Size, discount.End.ToDateTime()));
                    return 0;
                }

            case "create-item":
                await client.CreateCatalogItemAsync(ReadItem(commandLine, idIndex: null)).ConfigureAwait(false);
                Console.WriteLine("Created.");
                return 0;

            case "update-item":
                await client.UpdateCatalogItemAsync(ReadItem(commandLine, idIndex: 0)).ConfigureAwait(false);
                Console.WriteLine("Updated.");
                return 0;

            case "remove-item":
                await client.RemoveCatalogItemAsync(new CatalogItem
                {
                    Id = ValueParsing.ParsePositiveInt(commandLine.Required(0, "id"), "id"),
                }).ConfigureAwait(false);
                Console.WriteLine("Removed.");
                return 0;

            case "catalog":
                await RunCatalogTabAsync(client, ReadFilters(commandLine)).ConfigureAwait(false);
                return 0;

            case "inventory":
                {
                    var itemId = ValueParsing.ParsePositiveInt(commandLine.Required(0, "itemId"), "itemId");
                    var date = ValueParsing.ParseDate(commandLine.Required(1, "date"), "date");
                    var quantity = ValueParsing.ParseNonNegativeInt(commandLine.Required(2, "quantity"), "quantity");

                    await RunInventoryTabAsync(client, itemId, date, quantity).ConfigureAwait(false);
                    return 0;
                }

            default:
                return await UsageErrorAsync($"Command '{commandLine.Verb}' is not implemented.").ConfigureAwait(false);
        }
    }

    /// <summary>
    /// The WinForms "Main Catalog" tab as it loaded: the two filter drop-downs, today's discount
    /// banner and the product grid with the discount applied to every price.
    /// </summary>
    private static async Task RunCatalogTabAsync(
        CatalogService.CatalogServiceClient client,
        (int BrandId, int TypeId) filters)
    {
        await PrintBrandsAsync(client).ConfigureAwait(false);
        await PrintTypesAsync(client).ConfigureAwait(false);

        var discount = await FindDiscountAsync(client, DateTime.UtcNow.Date).ConfigureAwait(false);

        Console.WriteLine(discount is null
            ? CatalogFormatting.NoDiscountMessage
            : CatalogFormatting.FormatDiscountBanner(discount.Size, discount.End.ToDateTime()));
        Console.WriteLine();

        await PrintItemsAsync(client, filters, discount?.Size).ConfigureAwait(false);
    }

    /// <summary>
    /// The WinForms "Inventory" tab: pick a product, add a shipment for a date, then search the
    /// availability back out.
    /// </summary>
    private static async Task RunInventoryTabAsync(
        CatalogService.CatalogServiceClient client,
        int catalogItemId,
        DateTime date,
        int quantity)
    {
        var items = await client.GetCatalogItemsAsync(new GetCatalogItemsRequest());

        Console.WriteLine("Products:");
        foreach (var item in items.Items)
        {
            Console.WriteLine("  " + CatalogFormatting.FormatShipmentChoice(item));
        }

        Console.WriteLine();
        await CreateStockAsync(client, catalogItemId, date, quantity).ConfigureAwait(false);
        await PrintStockAsync(client, catalogItemId, date).ConfigureAwait(false);
    }

    private static async Task PrintBrandsAsync(CatalogService.CatalogServiceClient client)
    {
        var response = await client.GetCatalogBrandsAsync(new Empty());

        Console.WriteLine("Brands:");
        foreach (var brand in response.Brands)
        {
            Console.WriteLine("  " + CatalogFormatting.FormatBrand(brand));
        }
    }

    private static async Task PrintTypesAsync(CatalogService.CatalogServiceClient client)
    {
        var response = await client.GetCatalogTypesAsync(new Empty());

        Console.WriteLine("Types:");
        foreach (var type in response.CatalogTypes)
        {
            Console.WriteLine("  " + CatalogFormatting.FormatType(type));
        }
    }

    private static async Task PrintItemsAsync(
        CatalogService.CatalogServiceClient client,
        (int BrandId, int TypeId) filters,
        double? discount)
    {
        var response = await client.GetCatalogItemsAsync(new GetCatalogItemsRequest
        {
            BrandIdFilter = filters.BrandId,
            TypeIdFilter = filters.TypeId,
        });

        Console.WriteLine(FormattableString.Invariant($"Items ({response.Items.Count}):"));
        foreach (var item in response.Items)
        {
            Console.WriteLine("  " + (discount is { } fraction
                ? CatalogFormatting.FormatDiscountedItem(item, fraction)
                : CatalogFormatting.FormatItem(item)));
        }
    }

    private static async Task PrintStockAsync(
        CatalogService.CatalogServiceClient client,
        int catalogItemId,
        DateTime date)
    {
        var response = await client.GetAvailableStockAsync(new GetAvailableStockRequest
        {
            CatalogItemId = catalogItemId,
            Date = Timestamp.FromDateTime(date),
        });

        Console.WriteLine(CatalogFormatting.FormatStockAvailability(date, catalogItemId, response.AvailableStock));
    }

    private static async Task CreateStockAsync(
        CatalogService.CatalogServiceClient client,
        int catalogItemId,
        DateTime date,
        int quantity)
    {
        await client.CreateAvailableStockAsync(new CatalogItemsStock
        {
            CatalogItemId = catalogItemId,
            Date = Timestamp.FromDateTime(date),
            AvailableStock = quantity,
        }).ConfigureAwait(false);

        // The WinForms client showed a "Shipment has been added to the database." message box here.
        Console.WriteLine("Shipment has been added to the database.");
    }

    /// <summary>
    /// The legacy service answered <c>GetDiscount</c> with null when nothing was running and the
    /// WinForms client left its banner empty; over gRPC that null is <c>NOT_FOUND</c> (D-04).
    /// </summary>
    private static async Task<DiscountItem?> FindDiscountAsync(CatalogService.CatalogServiceClient client, DateTime day)
    {
        try
        {
            return await client.GetDiscountAsync(new GetDiscountRequest { Day = Timestamp.FromDateTime(day) });
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    private static (int BrandId, int TypeId) ReadFilters(CommandLine commandLine)
        => (
            commandLine.Optional(0) is { } brand ? ValueParsing.ParseNonNegativeInt(brand, "brandId") : 0,
            commandLine.Optional(1) is { } type ? ValueParsing.ParseNonNegativeInt(type, "typeId") : 0);

    private static CatalogItem ReadItem(CommandLine commandLine, int? idIndex)
    {
        var offset = idIndex.HasValue ? idIndex.Value + 1 : 0;

        var item = new CatalogItem
        {
            Name = commandLine.Required(offset, "name"),
            Price = new DecimalValue
            {
                Value = ValueParsing.ParseDecimal(commandLine.Required(offset + 1, "price"), "price")
                    .ToString(System.Globalization.CultureInfo.InvariantCulture),
            },
            CatalogBrandId = ValueParsing.ParsePositiveInt(commandLine.Required(offset + 2, "brandId"), "brandId"),
            CatalogTypeId = ValueParsing.ParsePositiveInt(commandLine.Required(offset + 3, "typeId"), "typeId"),
            Description = commandLine.Optional(offset + 4) ?? string.Empty,
            PictureFileName = commandLine.Optional(offset + 5) ?? string.Empty,
        };

        if (idIndex.HasValue)
        {
            item.Id = ValueParsing.ParsePositiveInt(commandLine.Required(idIndex.Value, "id"), "id");
        }

        return item;
    }

    private static async Task<int> UsageErrorAsync(string message)
    {
        await Console.Error.WriteLineAsync(message).ConfigureAwait(false);
        await Console.Error.WriteLineAsync().ConfigureAwait(false);
        await Console.Error.WriteAsync(CommandCatalog.UsageText).ConfigureAwait(false);
        return UsageExitCode;
    }
}
