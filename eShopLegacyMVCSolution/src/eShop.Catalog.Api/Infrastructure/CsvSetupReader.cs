using System.Globalization;
using System.Text.RegularExpressions;
using eShop.Catalog.Api.Model;

namespace eShop.Catalog.Api.Infrastructure;

/// <summary>Reads the optional Setup/*.csv customization files. Returns null when the file is absent.</summary>
public static class CsvSetupReader
{
    private static readonly Regex CsvSplit = new(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)", RegexOptions.Compiled);

    public static List<CatalogType>? ReadTypes(string path)
    {
        if (!File.Exists(path)) return null;
        GetHeaders(path, new[] { "catalogtype" });
        return File.ReadAllLines(path).Skip(1)
            .Select(l => l.Trim('"').Trim())
            .Where(l => l.Length > 0)
            .Select(l => new CatalogType { Type = l })
            .ToList();
    }

    public static List<CatalogBrand>? ReadBrands(string path)
    {
        if (!File.Exists(path)) return null;
        GetHeaders(path, new[] { "catalogbrand" });
        return File.ReadAllLines(path).Skip(1)
            .Select(l => l.Trim('"').Trim())
            .Where(l => l.Length > 0)
            .Select(l => new CatalogBrand { Brand = l })
            .ToList();
    }

    public static List<CatalogItem>? ReadItems(string path, IReadOnlyDictionary<string, int> typeIds, IReadOnlyDictionary<string, int> brandIds)
    {
        if (!File.Exists(path)) return null;
        string[] required = { "catalogtypename", "catalogbrandname", "description", "name", "price", "picturefilename" };
        string[] optional = { "availablestock", "restockthreshold", "maxstockthreshold", "onreorder" };
        var headers = GetHeaders(path, required, optional);

        return File.ReadAllLines(path).Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => CsvSplit.Split(l))
            .Select(c => CreateItem(c, headers, typeIds, brandIds))
            .ToList();
    }

    private static CatalogItem CreateItem(string[] column, string[] headers, IReadOnlyDictionary<string, int> typeIds, IReadOnlyDictionary<string, int> brandIds)
    {
        if (column.Length != headers.Length)
            throw new InvalidDataException($"column count '{column.Length}' not the same as headers count '{headers.Length}'");

        string Col(string name) => column[Array.IndexOf(headers, name)].Trim('"').Trim();

        var typeName = Col("catalogtypename");
        if (!typeIds.TryGetValue(typeName, out var typeId))
            throw new InvalidDataException($"type={typeName} does not exist in catalogTypes");

        var brandName = Col("catalogbrandname");
        if (!brandIds.TryGetValue(brandName, out var brandId))
            throw new InvalidDataException($"brand={brandName} does not exist in catalogBrands");

        var priceString = Col("price");
        if (!decimal.TryParse(priceString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var price))
            throw new InvalidDataException($"price={priceString} is not a valid decimal number");

        var item = new CatalogItem
        {
            CatalogTypeId = typeId,
            CatalogBrandId = brandId,
            Description = Col("description"),
            Name = Col("name"),
            Price = price,
            PictureFileName = Col("picturefilename"),
        };

        int OptionalInt(string name)
        {
            if (Array.IndexOf(headers, name) == -1 || Col(name).Length == 0) return 0;
            return int.TryParse(Col(name), out var v) ? v : throw new InvalidDataException($"{name}={Col(name)} is not a valid integer");
        }

        item.AvailableStock = OptionalInt("availablestock");
        item.RestockThreshold = OptionalInt("restockthreshold");
        item.MaxStockThreshold = OptionalInt("maxstockthreshold");
        if (Array.IndexOf(headers, "onreorder") != -1 && Col("onreorder").Length > 0)
        {
            item.OnReorder = bool.TryParse(Col("onreorder"), out var b) ? b : throw new InvalidDataException($"onreorder={Col("onreorder")} is not a valid boolean");
        }

        return item;
    }

    private static string[] GetHeaders(string path, string[] required, string[]? optional = null)
    {
        var headers = File.ReadLines(path).First().ToLowerInvariant().Split(',');
        if (headers.Length < required.Length)
            throw new InvalidDataException($"requiredHeader count '{required.Length}' is bigger then csv header count '{headers.Length}'");
        if (optional != null && headers.Length > required.Length + optional.Length)
            throw new InvalidDataException($"csv header count '{headers.Length}' is larger than required + optional headers");
        foreach (var h in required)
        {
            if (!headers.Contains(h)) throw new InvalidDataException($"does not contain required header '{h}'");
        }
        return headers;
    }
}
