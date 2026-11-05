using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace eShopWcf.Catalog.Data;

internal static class CatalogCsvSeed
{
    internal static CatalogSeed Load()
    {
        var brands = Read("CatalogBrands.csv").Select((r, i) =>
            new CatalogBrand { Id = i + 1, Brand = r[0].Trim() }).ToList();
        var types = Read("CatalogTypes.csv").Select((r, i) =>
            new CatalogType { Id = i + 1, Type = r[0].Trim() }).ToList();
        var items = new List<CatalogItem>();
        var stock = new List<CatalogItemsStock>();
        foreach (var row in Read("CatalogItems.csv"))
        {
            int id = items.Count + 1;
            items.Add(new CatalogItem
            {
                Id = id,
                CatalogTypeId = types.Single(x => x.Type == row[0].Trim()).Id,
                CatalogBrandId = brands.Single(x => x.Brand == row[1].Trim()).Id,
                Description = row[2].Trim(),
                Name = row[3].Trim(),
                Price = decimal.Parse(row[4].Trim(), CultureInfo.InvariantCulture),
                Picturefilename = row[5].Trim(),
            });
            stock.Add(new CatalogItemsStock
            {
                StockId = id,
                CatalogItemId = id,
                Date = new DateTime(2017, 9, 20),
                AvailableStock = int.Parse(row[6].Trim(), CultureInfo.InvariantCulture),
            });
        }

        return new CatalogSeed(brands, types, items, stock);
    }

    private static List<string[]> Read(string filename)
    {
        using var reader = File.OpenText(Path.Combine(AppContext.BaseDirectory, "Setup", filename));
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));
        csv.Read();
        csv.ReadHeader();
        var rows = new List<string[]>();
        while (csv.Read())
        {
            rows.Add(csv.Parser.Record!.ToArray());
        }

        return rows;
    }
}
