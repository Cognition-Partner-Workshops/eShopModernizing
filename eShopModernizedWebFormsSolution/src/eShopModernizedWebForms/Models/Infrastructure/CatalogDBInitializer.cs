using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace eShopModernizedWebForms.Models.Infrastructure
{
    /// <summary>
    /// Port of the legacy EF6 CreateDatabaseIfNotExists initializer: seeds only when the database
    /// is created, so an existing (legacy-created) catalog database is opened untouched.
    /// </summary>
    public class CatalogDBInitializer
    {
        private const string DBCatalogSequenceName = "catalog_type_hilo";
        private const string DBBrandSequenceName = "catalog_brand_hilo";
        private const string SequenceOptions = " AS [bigint] START WITH 1 INCREMENT BY 10 MINVALUE -9223372036854775808 MAXVALUE 9223372036854775807 CACHE";

        // Same objects as the legacy dbo.*_hilo.Sequence.sql scripts, minus their hard-coded USE [db] line.
        private static readonly string[] SequenceScripts =
        {
            "CREATE SEQUENCE [dbo].[catalog_hilo]" + SequenceOptions,
            "CREATE SEQUENCE [dbo].[catalog_brand_hilo]" + SequenceOptions,
            "CREATE SEQUENCE [dbo].[catalog_type_hilo]" + SequenceOptions,
        };

        private readonly CatalogItemHiLoGenerator indexGenerator;
        private readonly bool useCustomizationData;
        private readonly string contentRootPath;
        private readonly string webRootPath;

        public CatalogDBInitializer(CatalogItemHiLoGenerator indexGenerator, bool useCustomizationData, string contentRootPath, string webRootPath)
        {
            this.indexGenerator = indexGenerator;
            this.useCustomizationData = useCustomizationData;
            this.contentRootPath = contentRootPath;
            this.webRootPath = webRootPath;
        }

        public bool InitializeDatabase(CatalogDBContext context)
        {
            if (!context.Database.EnsureCreated())
            {
                return false;
            }

            Seed(context);
            return true;
        }

        private void Seed(CatalogDBContext context)
        {
            foreach (var script in SequenceScripts)
            {
                context.Database.ExecuteSqlRaw(script);
            }

            AddCatalogTypes(context);
            AddCatalogBrands(context);
            AddCatalogItems(context);
            AddCatalogItemPictures();
        }

        private void AddCatalogTypes(CatalogDBContext context)
        {
            var preconfiguredTypes = useCustomizationData
                ? GetCatalogTypesFromFile()
                : PreconfiguredData.GetPreconfiguredCatalogTypes();

            // The legacy code consumed one catalog_type_hilo value; the IDENTITY column assigns the ids.
            GetSequenceIdFromSelectedDBSequence(context, DBCatalogSequenceName);
            foreach (var type in preconfiguredTypes)
            {
                type.Id = 0;
                context.CatalogTypes.Add(type);
                context.SaveChanges();
            }
        }

        private void AddCatalogBrands(CatalogDBContext context)
        {
            var preconfiguredBrands = useCustomizationData
                ? GetCatalogBrandsFromFile()
                : PreconfiguredData.GetPreconfiguredCatalogBrands();

            GetSequenceIdFromSelectedDBSequence(context, DBBrandSequenceName);
            foreach (var brand in preconfiguredBrands)
            {
                brand.Id = 0;
                context.CatalogBrands.Add(brand);
                context.SaveChanges();
            }
        }

        private void AddCatalogItems(CatalogDBContext context)
        {
            var preconfiguredItems = useCustomizationData
                ? GetCatalogItemsFromFile(context)
                : PreconfiguredData.GetPreconfiguredCatalogItems();

            foreach (var item in preconfiguredItems)
            {
                item.Id = indexGenerator.GetNextSequenceValue(context);
                context.CatalogItems.Add(item);
            }

            context.SaveChanges();
        }

        private IEnumerable<CatalogType> GetCatalogTypesFromFile()
        {
            string csvFileCatalogTypes = Path.Combine(contentRootPath, "Setup", "CatalogTypes.csv");

            if (!File.Exists(csvFileCatalogTypes))
            {
                return PreconfiguredData.GetPreconfiguredCatalogTypes();
            }

            GetHeaders(csvFileCatalogTypes, new[] { "catalogtype" });

            return File.ReadAllLines(csvFileCatalogTypes)
                .Skip(1)
                .Select(CreateCatalogType)
                .Where(x => x != null)
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

        private IEnumerable<CatalogBrand> GetCatalogBrandsFromFile()
        {
            string csvFileCatalogBrands = Path.Combine(contentRootPath, "Setup", "CatalogBrands.csv");

            if (!File.Exists(csvFileCatalogBrands))
            {
                return PreconfiguredData.GetPreconfiguredCatalogBrands();
            }

            GetHeaders(csvFileCatalogBrands, new[] { "catalogbrand" });

            return File.ReadAllLines(csvFileCatalogBrands)
                .Skip(1)
                .Select(CreateCatalogBrand)
                .Where(x => x != null)
                .ToList();
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

        private IEnumerable<CatalogItem> GetCatalogItemsFromFile(CatalogDBContext context)
        {
            string csvFileCatalogItems = Path.Combine(contentRootPath, "Setup", "CatalogItems.csv");

            if (!File.Exists(csvFileCatalogItems))
            {
                return PreconfiguredData.GetPreconfiguredCatalogItems();
            }

            string[] requiredHeaders = { "catalogtypename", "catalogbrandname", "description", "name", "price", "pictureFileName" };
            string[] optionalheaders = { "availablestock", "restockthreshold", "maxstockthreshold", "onreorder" };
            string[] csvheaders = GetHeaders(csvFileCatalogItems, requiredHeaders, optionalheaders);

            var catalogTypeIdLookup = context.CatalogTypes.ToDictionary(ct => ct.Type, ct => ct.Id);
            var catalogBrandIdLookup = context.CatalogBrands.ToDictionary(ct => ct.Brand, ct => ct.Id);

            return File.ReadAllLines(csvFileCatalogItems)
                .Skip(1)
                .Select(row => Regex.Split(row, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)"))
                .Select(column => CreateCatalogItem(column, csvheaders, catalogTypeIdLookup, catalogBrandIdLookup))
                .Where(x => x != null)
                .ToList();
        }

        internal static CatalogItem CreateCatalogItem(string[] column, string[] headers, Dictionary<string, int> catalogTypeIdLookup, Dictionary<string, int> catalogBrandIdLookup)
        {
            if (column.Length != headers.Length)
            {
                throw new InvalidDataException($"column count '{column.Length}' not the same as headers count'{headers.Length}'");
            }

            string catalogTypeName = column[Array.IndexOf(headers, "catalogtypename")].Trim('"').Trim();
            if (!catalogTypeIdLookup.TryGetValue(catalogTypeName, out int catalogTypeId))
            {
                throw new InvalidDataException($"type={catalogTypeName} does not exist in catalogTypes");
            }

            string catalogBrandName = column[Array.IndexOf(headers, "catalogbrandname")].Trim('"').Trim();
            if (!catalogBrandIdLookup.TryGetValue(catalogBrandName, out int catalogBrandId))
            {
                throw new InvalidDataException($"type={catalogTypeName} does not exist in catalogTypes");
            }

            string priceString = column[Array.IndexOf(headers, "price")].Trim('"').Trim();
            if (!decimal.TryParse(priceString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal price))
            {
                throw new InvalidDataException($"price={priceString}is not a valid decimal number");
            }

            var catalogItem = new CatalogItem
            {
                CatalogTypeId = catalogTypeId,
                CatalogBrandId = catalogBrandId,
                Description = column[Array.IndexOf(headers, "description")].Trim('"').Trim(),
                Name = column[Array.IndexOf(headers, "name")].Trim('"').Trim(),
                Price = price,
                PictureFileName = column[Array.IndexOf(headers, "picturefilename")].Trim('"').Trim(),
            };

            int? availableStock = ParseOptionalInt(column, headers, "availablestock");
            if (availableStock.HasValue)
            {
                catalogItem.AvailableStock = availableStock.Value;
            }

            int? restockThreshold = ParseOptionalInt(column, headers, "restockthreshold");
            if (restockThreshold.HasValue)
            {
                catalogItem.RestockThreshold = restockThreshold.Value;
            }

            int? maxStockThreshold = ParseOptionalInt(column, headers, "maxstockthreshold");
            if (maxStockThreshold.HasValue)
            {
                catalogItem.MaxStockThreshold = maxStockThreshold.Value;
            }

            int onReorderIndex = Array.IndexOf(headers, "onreorder");
            if (onReorderIndex != -1)
            {
                string onReorderString = column[onReorderIndex].Trim('"').Trim();
                if (!string.IsNullOrEmpty(onReorderString))
                {
                    if (!bool.TryParse(onReorderString, out bool onReorder))
                    {
                        throw new InvalidDataException($"onReorder={onReorderString} is not a valid boolean");
                    }

                    catalogItem.OnReorder = onReorder;
                }
            }

            return catalogItem;
        }

        private static int? ParseOptionalInt(string[] column, string[] headers, string header)
        {
            int index = Array.IndexOf(headers, header);
            if (index == -1)
            {
                return null;
            }

            string value = column[index].Trim('"').Trim();
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (!int.TryParse(value, out int result))
            {
                throw new InvalidDataException($"{header}={value} is not a valid integer");
            }

            return result;
        }

        internal static string[] GetHeaders(string csvfile, string[] requiredHeaders, string[] optionalHeaders = null)
        {
            string[] csvheaders = File.ReadLines(csvfile).First().ToLowerInvariant().Split(',');

            if (csvheaders.Length < requiredHeaders.Length)
            {
                throw new InvalidDataException($"requiredHeader count '{requiredHeaders.Length}' is bigger then csv header count '{csvheaders.Length}' ");
            }

            if (optionalHeaders != null && csvheaders.Length > (requiredHeaders.Length + optionalHeaders.Length))
            {
                throw new InvalidDataException($"csv header count '{csvheaders.Length}'  is larger then required '{requiredHeaders.Length}' and optional '{optionalHeaders.Length}' headers count");
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

        private static int GetSequenceIdFromSelectedDBSequence(CatalogDBContext context, string dBSequenceName)
        {
            string query = dBSequenceName == DBBrandSequenceName
                ? "SELECT NEXT VALUE FOR catalog_brand_hilo AS [Value]"
                : "SELECT NEXT VALUE FOR catalog_type_hilo AS [Value]";
            return (int)context.Database
                .SqlQueryRaw<long>(query)
                .AsEnumerable()
                .Single();
        }

        private void AddCatalogItemPictures()
        {
            if (!useCustomizationData)
            {
                return;
            }

            var picturePath = new DirectoryInfo(Path.Combine(webRootPath, "Pics"));
            foreach (FileInfo file in picturePath.GetFiles())
            {
                file.Delete();
            }

            string zipFileCatalogItemPictures = Path.Combine(contentRootPath, "Setup", "CatalogItems.zip");
            ZipFile.ExtractToDirectory(zipFileCatalogItemPictures, picturePath.FullName);
        }
    }
}
