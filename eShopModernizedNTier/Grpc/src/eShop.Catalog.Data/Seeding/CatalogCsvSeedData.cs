using System.Globalization;
using System.Text.RegularExpressions;
using eShop.Catalog.Domain;

namespace eShop.Catalog.Data.Seeding;

/// <summary>
/// Port of the CSV readers in the legacy <c>CatalogDBInitializer</c>
/// (<c>GetCatalogTypesFromFile</c> / <c>GetCatalogBrandsFromFile</c> / <c>GetCatalogItemsFromFile</c>),
/// including their header validation, quote trimming and error messages. A missing file silently
/// falls back to the preconfigured data, exactly as the legacy code did.
/// </summary>
public static class CatalogCsvSeedData
{
    /// <summary>File names the legacy applications shipped under <c>Setup/</c>.</summary>
    public const string CatalogTypesFileName = "CatalogTypes.csv";

    /// <inheritdoc cref="CatalogTypesFileName"/>
    public const string CatalogBrandsFileName = "CatalogBrands.csv";

    /// <inheritdoc cref="CatalogTypesFileName"/>
    public const string CatalogItemsFileName = "CatalogItems.csv";

    /// <inheritdoc cref="CatalogTypesFileName"/>
    public const string CatalogItemPicturesFileName = "CatalogItems.zip";

    private static readonly Regex CsvColumnSeparator =
        new(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)", RegexOptions.Compiled);

    public static IEnumerable<CatalogType> GetCatalogTypes(string setupDirectory)
    {
        var file = Path.Combine(setupDirectory, CatalogTypesFileName);
        if (!File.Exists(file))
        {
            return PreconfiguredData.GetPreconfiguredCatalogTypes();
        }

        GetHeaders(file, ["catalogtype"]);

        return File.ReadAllLines(file)
            .Skip(1)
            .Select(CreateCatalogType)
            .ToList();
    }

    public static IEnumerable<CatalogBrand> GetCatalogBrands(string setupDirectory)
    {
        var file = Path.Combine(setupDirectory, CatalogBrandsFileName);
        if (!File.Exists(file))
        {
            return PreconfiguredData.GetPreconfiguredCatalogBrands();
        }

        GetHeaders(file, ["catalogbrand"]);

        return File.ReadAllLines(file)
            .Skip(1)
            .Select(CreateCatalogBrand)
            .ToList();
    }

    public static IEnumerable<CatalogItem> GetCatalogItems(
        string setupDirectory,
        IReadOnlyDictionary<string, int> catalogTypeIdLookup,
        IReadOnlyDictionary<string, int> catalogBrandIdLookup)
    {
        var file = Path.Combine(setupDirectory, CatalogItemsFileName);
        if (!File.Exists(file))
        {
            return PreconfiguredData.GetPreconfiguredCatalogItems();
        }

        string[] requiredHeaders =
            ["catalogtypename", "catalogbrandname", "description", "name", "price", "picturefilename"];
        string[] optionalHeaders = ["availablestock", "restockthreshold", "maxstockthreshold", "onreorder"];
        var headers = GetHeaders(file, requiredHeaders, optionalHeaders);

        return File.ReadAllLines(file)
            .Skip(1)
            .Select(row => CsvColumnSeparator.Split(row))
            .Select(column => CreateCatalogItem(column, headers, catalogTypeIdLookup, catalogBrandIdLookup))
            .ToList();
    }

    private static CatalogType CreateCatalogType(string type)
    {
        type = type.Trim('"').Trim();

        if (string.IsNullOrEmpty(type))
        {
            throw new InvalidOperationException("catalog Type Name is empty");
        }

        return new CatalogType { Type = type };
    }

    private static CatalogBrand CreateCatalogBrand(string brand)
    {
        brand = brand.Trim('"').Trim();

        if (string.IsNullOrEmpty(brand))
        {
            throw new InvalidOperationException("catalog Brand Name is empty");
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
            throw new InvalidOperationException(
                $"column count '{column.Length}' not the same as headers count'{headers.Length}'");
        }

        var catalogTypeName = Value(column, headers, "catalogtypename");
        if (!catalogTypeIdLookup.ContainsKey(catalogTypeName))
        {
            throw new InvalidOperationException($"type={catalogTypeName} does not exist in catalogTypes");
        }

        var catalogBrandName = Value(column, headers, "catalogbrandname");
        if (!catalogBrandIdLookup.ContainsKey(catalogBrandName))
        {
            throw new InvalidOperationException($"brand={catalogBrandName} does not exist in catalogBrands");
        }

        var priceString = Value(column, headers, "price");
        if (!decimal.TryParse(priceString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var price))
        {
            throw new InvalidOperationException($"price={priceString}is not a valid decimal number");
        }

        var catalogItem = new CatalogItem
        {
            CatalogTypeId = catalogTypeIdLookup[catalogTypeName],
            CatalogBrandId = catalogBrandIdLookup[catalogBrandName],
            Description = Value(column, headers, "description"),
            Name = Value(column, headers, "name"),
            Price = price,
            PictureFileName = Value(column, headers, "picturefilename"),
        };

        if (TryGetOptional(column, headers, "availablestock", out var availableStockString))
        {
            catalogItem.AvailableStock = ParseInt32("availableStock", availableStockString);
        }

        if (TryGetOptional(column, headers, "restockthreshold", out var restockThresholdString))
        {
            catalogItem.RestockThreshold = ParseInt32("restockThreshold", restockThresholdString);
        }

        if (TryGetOptional(column, headers, "maxstockthreshold", out var maxStockThresholdString))
        {
            catalogItem.MaxStockThreshold = ParseInt32("maxStockThreshold", maxStockThresholdString);
        }

        if (TryGetOptional(column, headers, "onreorder", out var onReorderString))
        {
            catalogItem.OnReorder = bool.TryParse(onReorderString, out var onReorder)
                ? onReorder
                : throw new InvalidOperationException($"onReorder={onReorderString} is not a valid boolean");
        }

        return catalogItem;
    }

    private static string Value(string[] column, string[] headers, string header) =>
        column[Array.IndexOf(headers, header)].Trim('"').Trim();

    private static bool TryGetOptional(string[] column, string[] headers, string header, out string value)
    {
        value = string.Empty;

        var index = Array.IndexOf(headers, header);
        if (index == -1)
        {
            return false;
        }

        value = column[index].Trim('"').Trim();

        return !string.IsNullOrEmpty(value);
    }

    private static int ParseInt32(string header, string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"{header}={value} is not a valid integer");

    private static string[] GetHeaders(string csvFile, string[] requiredHeaders, string[]? optionalHeaders = null)
    {
        var csvHeaders = File.ReadLines(csvFile).First().ToLowerInvariant().Split(',');

        if (csvHeaders.Length < requiredHeaders.Length)
        {
            throw new InvalidOperationException(
                $"requiredHeader count '{requiredHeaders.Length}' is bigger then csv header count '{csvHeaders.Length}' ");
        }

        if (optionalHeaders is not null && csvHeaders.Length > requiredHeaders.Length + optionalHeaders.Length)
        {
            throw new InvalidOperationException(
                $"csv header count '{csvHeaders.Length}'  is larger then required '{requiredHeaders.Length}' " +
                $"and optional '{optionalHeaders.Length}' headers count");
        }

        foreach (var requiredHeader in requiredHeaders)
        {
            if (!csvHeaders.Contains(requiredHeader.ToLowerInvariant()))
            {
                throw new InvalidOperationException($"does not contain required header '{requiredHeader}'");
            }
        }

        return csvHeaders;
    }
}
