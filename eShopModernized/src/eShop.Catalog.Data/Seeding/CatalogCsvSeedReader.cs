using System.Globalization;
using System.Text.RegularExpressions;
using eShop.Catalog.Data.Infrastructure;
using eShop.Catalog.Domain.Entities;

namespace eShop.Catalog.Data.Seeding;

/// <summary>
/// Port of the CSV customization reader embedded in the legacy
/// <c>CatalogDBInitializer</c>. Column contracts, trimming, the invariant-culture price parse and
/// the exception messages are kept as they were; a missing file falls back to
/// <see cref="PreconfiguredData"/>, a malformed one throws, exactly as before.
/// </summary>
internal static partial class CatalogCsvSeedReader
{
    public const string CatalogTypesFileName = "CatalogTypes.csv";
    public const string CatalogBrandsFileName = "CatalogBrands.csv";
    public const string CatalogItemsFileName = "CatalogItems.csv";

    /// <summary>Splits on commas that are not inside a quoted field.</summary>
    [GeneratedRegex(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)")]
    private static partial Regex CsvColumnSeparator();

    public static IEnumerable<CatalogType> GetCatalogTypes(string setupFolder)
    {
        var csvFileCatalogTypes = Path.Combine(setupFolder, CatalogTypesFileName);

        if (!File.Exists(csvFileCatalogTypes))
        {
            return PreconfiguredData.GetPreconfiguredCatalogTypes();
        }

        GetHeaders(csvFileCatalogTypes, ["catalogtype"]);

        return File.ReadAllLines(csvFileCatalogTypes)
            .Skip(1)
            .Select(CreateCatalogType)
            .ToList();
    }

    public static IEnumerable<CatalogBrand> GetCatalogBrands(string setupFolder)
    {
        var csvFileCatalogBrands = Path.Combine(setupFolder, CatalogBrandsFileName);

        if (!File.Exists(csvFileCatalogBrands))
        {
            return PreconfiguredData.GetPreconfiguredCatalogBrands();
        }

        GetHeaders(csvFileCatalogBrands, ["catalogbrand"]);

        return File.ReadAllLines(csvFileCatalogBrands)
            .Skip(1)
            .Select(CreateCatalogBrand)
            .ToList();
    }

    public static IEnumerable<CatalogItem> GetCatalogItems(
        string setupFolder,
        IReadOnlyDictionary<string, int> catalogTypeIdLookup,
        IReadOnlyDictionary<string, int> catalogBrandIdLookup)
    {
        var csvFileCatalogItems = Path.Combine(setupFolder, CatalogItemsFileName);

        if (!File.Exists(csvFileCatalogItems))
        {
            return PreconfiguredData.GetPreconfiguredCatalogItems();
        }

        string[] requiredHeaders =
            ["catalogtypename", "catalogbrandname", "description", "name", "price", "pictureFileName"];
        string[] optionalHeaders = ["availablestock", "restockthreshold", "maxstockthreshold", "onreorder"];
        var csvheaders = GetHeaders(csvFileCatalogItems, requiredHeaders, optionalHeaders);

        return File.ReadAllLines(csvFileCatalogItems)
            .Skip(1)
            .Select(row => CsvColumnSeparator().Split(row))
            .Select(column => CreateCatalogItem(column, csvheaders, catalogTypeIdLookup, catalogBrandIdLookup))
            .ToList();
    }

    private static CatalogType CreateCatalogType(string type)
    {
        type = type.Trim('"').Trim();

        if (string.IsNullOrEmpty(type))
        {
            throw new InvalidDataException("catalog Type Name is empty");
        }

        return new CatalogType { Type = type };
    }

    private static CatalogBrand CreateCatalogBrand(string brand)
    {
        brand = brand.Trim('"').Trim();

        if (string.IsNullOrEmpty(brand))
        {
            throw new InvalidDataException("catalog Brand Name is empty");
        }

        return new CatalogBrand { Brand = brand };
    }

    private static CatalogItem CreateCatalogItem(
        string[] column,
        string[] headers,
        IReadOnlyDictionary<string, int> catalogTypeIdLookup,
        IReadOnlyDictionary<string, int> catalogBrandIdLookup)
    {
        if (column.Length != headers.Length)
        {
            throw new InvalidDataException(
                $"column count '{column.Length}' not the same as headers count'{headers.Length}'");
        }

        var catalogTypeName = Value(column, headers, "catalogtypename");
        if (!catalogTypeIdLookup.TryGetValue(catalogTypeName, out var catalogTypeId))
        {
            throw new InvalidDataException($"type={catalogTypeName} does not exist in catalogTypes");
        }

        var catalogBrandName = Value(column, headers, "catalogbrandname");
        if (!catalogBrandIdLookup.TryGetValue(catalogBrandName, out var catalogBrandId))
        {
            throw new InvalidDataException($"type={catalogTypeName} does not exist in catalogTypes");
        }

        var priceString = Value(column, headers, "price");
        if (!decimal.TryParse(priceString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var price))
        {
            throw new InvalidDataException($"price={priceString}is not a valid decimal number");
        }

        var catalogItem = new CatalogItem
        {
            CatalogTypeId = catalogTypeId,
            CatalogBrandId = catalogBrandId,
            Description = Value(column, headers, "description"),
            Name = Value(column, headers, "name"),
            Price = price,
            PictureFileName = Value(column, headers, "picturefilename"),
        };

        if (OptionalValue(column, headers, "availablestock") is { } availableStockString)
        {
            catalogItem.AvailableStock = int.TryParse(availableStockString, CultureInfo.InvariantCulture, out var availableStock)
                ? availableStock
                : throw new InvalidDataException($"availableStock={availableStockString} is not a valid integer");
        }

        if (OptionalValue(column, headers, "restockthreshold") is { } restockThresholdString)
        {
            catalogItem.RestockThreshold = int.TryParse(restockThresholdString, CultureInfo.InvariantCulture, out var restockThreshold)
                ? restockThreshold
                : throw new InvalidDataException($"restockThreshold={restockThresholdString} is not a valid integer");
        }

        if (OptionalValue(column, headers, "maxstockthreshold") is { } maxStockThresholdString)
        {
            catalogItem.MaxStockThreshold = int.TryParse(maxStockThresholdString, CultureInfo.InvariantCulture, out var maxStockThreshold)
                ? maxStockThreshold
                : throw new InvalidDataException($"maxStockThreshold={maxStockThresholdString} is not a valid integer");
        }

        if (OptionalValue(column, headers, "onreorder") is { } onReorderString)
        {
            catalogItem.OnReorder = bool.TryParse(onReorderString, out var onReorder)
                ? onReorder
                : throw new InvalidDataException($"onReorder={onReorderString} is not a valid boolean");
        }

        return catalogItem;
    }

    private static string Value(string[] column, string[] headers, string header) =>
        column[Array.IndexOf(headers, header)].Trim('"').Trim();

    private static string? OptionalValue(string[] column, string[] headers, string header)
    {
        var index = Array.IndexOf(headers, header);
        if (index == -1)
        {
            return null;
        }

        var value = column[index].Trim('"').Trim();

        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static string[] GetHeaders(string csvfile, string[] requiredHeaders, string[]? optionalHeaders = null)
    {
        var csvheaders = File.ReadLines(csvfile).First().ToLowerInvariant().Split(',');

        if (csvheaders.Length < requiredHeaders.Length)
        {
            throw new InvalidDataException(
                $"requiredHeader count '{requiredHeaders.Length}' is bigger then csv header count '{csvheaders.Length}' ");
        }

        if (optionalHeaders is not null && csvheaders.Length > requiredHeaders.Length + optionalHeaders.Length)
        {
            throw new InvalidDataException(
                $"csv header count '{csvheaders.Length}'  is larger then required '{requiredHeaders.Length}' and optional '{optionalHeaders.Length}' headers count");
        }

        foreach (var requiredHeader in requiredHeaders)
        {
            if (!csvheaders.Contains(requiredHeader.ToLowerInvariant()))
            {
                throw new InvalidDataException($"does not contain required header '{requiredHeader}'");
            }
        }

        return csvheaders;
    }
}
