using System.Security.Cryptography;
using System.Text.Json;
using eShopModernizedMVC.Models.Infrastructure;
using Microsoft.VisualBasic.FileIO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eShopModernizedMVC.ParityTests;

[TestClass]
public class SeedVariantTests
{
    [TestMethod]
    public void CanonicalCsvAndPreconfiguredVariantsAreRecorded()
    {
        if(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LEGACY_BASE_URL")) || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MODERN_BASE_URL")))Assert.Inconclusive("Set both base URLs to enable the parity harness.");
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        var setup = Path.Combine(root,"eShopLegacyMVCSolution/src/eShopLegacyMVC/Setup");
        var modern = Path.Combine(root,"eShopModernizedMVCSolution/src/eShopModernizedMVC/Setup");
        if(!Directory.Exists(setup))Assert.Inconclusive("Run from a checkout containing both solutions.");
        var evidence = Environment.GetEnvironmentVariable("PARITY_EVIDENCE");
        var differences=new List<object>();
        var pre=PreconfiguredData.GetPreconfiguredCatalogItems();
        var legacySource=File.ReadAllText(Path.Combine(root,"eShopLegacyMVCSolution/src/eShopLegacyMVC/Models/Infrastructure/PreconfiguredData.cs")).Replace("using System.Web;","").Replace("eShopLegacyMVC","APP");
        var modernSource=File.ReadAllText(Path.Combine(root,"eShopModernizedMVCSolution/src/eShopModernizedMVC/Models/Infrastructure/PreconfiguredData.cs")).Replace("eShopModernizedMVC","APP");
        Assert.AreEqual(System.Text.RegularExpressions.Regex.Replace(legacySource,@"\s+"," ").Trim(),System.Text.RegularExpressions.Regex.Replace(modernSource,@"\s+"," ").Trim(),"Preconfigured seed bodies differ between applications");
        using var csv=new TextFieldParser(Path.Combine(setup,"CatalogItems.csv"));
        csv.SetDelimiters(",");csv.HasFieldsEnclosedInQuotes=true;
        var headers=csv.ReadFields();
        while(!csv.EndOfData)
        {
            var fields=csv.ReadFields();var row=headers.Zip(fields).ToDictionary(x=>x.First.ToLowerInvariant(),x=>x.Second.Trim());
            var item=pre.SingleOrDefault(x=>x.Name==row["name"]);
            if(item==null){differences.Add(new {name=row["name"],field="row",preconfigured="absent",csv="present"});continue;}
            foreach(var (field,value) in new[]{("description",item.Description),("availablestock",item.AvailableStock.ToString()),("onreorder",item.OnReorder.ToString().ToLowerInvariant())})
                if(value!=row[field])differences.Add(new{name=item.Name,field,preconfigured=value,csv=row[field]});
        }
        foreach(var name in new[]{"CatalogItems.csv","CatalogBrands.csv","CatalogTypes.csv","CatalogItems.zip"})
            CollectionAssert.AreEqual(SHA256.HashData(File.ReadAllBytes(Path.Combine(setup,name))),SHA256.HashData(File.ReadAllBytes(Path.Combine(modern,name))),name);
        Assert.AreEqual(13,File.ReadAllLines(Path.Combine(setup,"CatalogItems.csv")).Length-1);
        Assert.AreEqual(12,pre.Count);
        foreach(var (file,field,expected) in new[]{("CatalogBrands.csv","Brand",PreconfiguredData.GetPreconfiguredCatalogBrands().Select(x=>x.Brand).ToArray()),("CatalogTypes.csv","Type",PreconfiguredData.GetPreconfiguredCatalogTypes().Select(x=>x.Type).ToArray())})
        {
            var actual=File.ReadAllLines(Path.Combine(setup,file)).Skip(1).Select(x=>x.Trim().Trim('"')).ToArray();
            foreach(var name in actual.Except(expected))differences.Add(new{name,field,preconfigured="absent",csv="present"});
            foreach(var name in expected.Except(actual))differences.Add(new{name,field,preconfigured="present",csv="absent"});
        }
        if(evidence!=null){Directory.CreateDirectory(evidence);File.WriteAllText(Path.Combine(evidence,"seed-variants.json"),JsonSerializer.Serialize(differences,new JsonSerializerOptions{WriteIndented=true}));File.WriteAllText(Path.Combine(evidence,"preconfigured-items.json"),JsonSerializer.Serialize(pre,new JsonSerializerOptions{WriteIndented=true}));}
        TestContext.WriteLine(JsonSerializer.Serialize(differences));
    }
    public TestContext TestContext {get;set;}
}
