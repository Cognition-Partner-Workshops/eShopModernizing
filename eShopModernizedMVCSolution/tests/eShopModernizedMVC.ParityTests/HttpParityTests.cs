using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eShopModernizedMVC.ParityTests;

[TestClass]
public class HttpParityTests
{
    readonly List<object> observations = new();
    readonly List<string> failures = new();
    string evidence;
    record Result(int Status, string ContentType, int Length, string Value);
    sealed class Site : IDisposable
    {
        public readonly HttpClient Client;
        public readonly string Database;
        public Site(string url, string database)
        {
            Client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() })
                { BaseAddress = new Uri(url), Timeout = TimeSpan.FromSeconds(45) };
            Database = database;
        }
        public void Dispose() => Client.Dispose();
    }
    static IDocument Html(string text) => new HtmlParser().ParseDocument(text);
    static string Text(IElement node) => System.Text.RegularExpressions.Regex.Replace(node.TextContent, @"\s+", " ").Trim();
    static string Json(object value) => JsonSerializer.Serialize(value);
    static string Normalize(string body, string kind)
    {
        var doc = Html(body);
        return kind switch
        {
            "index" => Json(doc.QuerySelectorAll("tbody tr").Select(row => row.QuerySelectorAll("td").Skip(1).SkipLast(1).Select(Text).ToArray()).ToArray()),
            "details" => Json(doc.QuerySelectorAll("dl dd").Select(Text).ToArray()),
            "form" => Json(doc.QuerySelectorAll("input,select,textarea").Where(x => x.GetAttribute("name") != "__RequestVerificationToken")
                .Where(x => x.HasAttribute("name")).Select(x => new { name = x.GetAttribute("name"), value = x.LocalName == "select" ? string.Join("|", x.QuerySelectorAll("option").Select(Text)) : x.GetAttribute("value") ?? x.TextContent.Trim() }).ToArray()),
            "validation" => Json(doc.QuerySelectorAll(".field-validation-error").Select(x => x.GetAttribute("data-valmsg-for")).OrderBy(x => x).ToArray()),
            "json" => Json(JsonSerializer.Deserialize<JsonElement>(body)),
            _ => ""
        };
    }
    async Task<Result> Request(Site site, string path, string kind = "", Dictionary<string,string> form = null, string method = "GET", string accept = null)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (form != null) request.Content = new FormUrlEncodedContent(form);
        if (accept != null) request.Headers.Accept.ParseAdd(accept);
        using var response = await site.Client.SendAsync(request);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var type = response.Content.Headers.ContentType?.MediaType ?? "";
        var status = (int)response.StatusCode;
        var value = kind == "image" && status == 200 ? Convert.ToHexString(SHA256.HashData(bytes)) : status == 200 && kind != "file" ? Normalize(System.Text.Encoding.UTF8.GetString(bytes), kind) : "";
        return new Result(status, type, bytes.Length, value);
    }
    void Compare(string operation, Result legacy, Result modern, string difference = null, int? expectedStatus = null)
    {
        var transportDifference = legacy.Status == modern.Status && legacy.Status != 200 && legacy.ContentType != modern.ContentType;
        observations.Add(new { operation, legacy, modern, classification = difference ?? (transportDifference ? "platform: IIS HTML error/redirect body versus empty Core response" : "equivalent") });
        if (expectedStatus != null && (legacy.Status != expectedStatus || modern.Status != expectedStatus)) failures.Add($"{operation}: expected {expectedStatus}, got {legacy.Status}/{modern.Status}");
        if(operation.StartsWith("antiforgery") && (legacy.Status!=500 || modern.Status!=400))failures.Add(operation+": unexpected antiforgery status");
        if(operation=="/api" && (legacy.Status!=404 || modern.Status!=200 || modern.Value!="{\"Message\":\"Hello World!\"}"))failures.Add("Unexpected /api response");
        if(operation is "/api/files" or "XML brands" && (legacy.Status!=200 || modern.Status!=200))failures.Add(operation+": failed endpoint");
        if (difference == null && (legacy.Status != modern.Status || (legacy.Status == 200 && legacy.ContentType != modern.ContentType) || legacy.Value != modern.Value)) failures.Add($"{operation}: {Json(legacy)} != {Json(modern)}");
    }
    static async Task<Dictionary<string,string>> Form(Site site, string path)
    {
        var html = Html(await site.Client.GetStringAsync(path));
        return html.QuerySelectorAll("input[name]").Where(x => x.GetAttribute("type") != "checkbox")
            .GroupBy(x => x.GetAttribute("name")).ToDictionary(x => x.Key, x => x.First().GetAttribute("value") ?? "");
    }
    static Dictionary<string,string> Product(string name, int id = 0) => new()
    {
        ["Id"] = id.ToString(), ["Name"] = name, ["Description"] = "Parity persisted description", ["Price"] = "23.45",
        ["CatalogBrandId"] = "2", ["CatalogTypeId"] = "2", ["PictureFileName"] = "1.png", ["AvailableStock"] = "42",
        ["RestockThreshold"] = "3", ["MaxStockThreshold"] = "100", ["OnReorder"] = "false"
    };
    static async Task<string> Data(Site site, bool ids = false)
    {
        await using var c = new SqlConnection($"Server=(localdb)\\MSSQLLocalDB;Database={site.Database};Integrated Security=True;TrustServerCertificate=True");
        await c.OpenAsync();
        var columns = "c.Name,c.Description,c.Price,c.PictureFileName,c.AvailableStock,c.RestockThreshold,c.MaxStockThreshold,c.OnReorder,b.Brand,t.Type";
        await using var q = new SqlCommand($"SELECT {(ids ? "c.Id," : "")}{columns} FROM Catalog c JOIN CatalogBrand b ON b.Id=c.CatalogBrandId JOIN CatalogType t ON t.Id=c.CatalogTypeId ORDER BY c.Name,c.Description,c.Price", c);
        var rows = new List<object[]>();
        await using (var r = await q.ExecuteReaderAsync())
            while (await r.ReadAsync()) { var row = new object[r.FieldCount]; r.GetValues(row); rows.Add(row.Select(x => x is DBNull ? null : x).ToArray()); }
        var lookups=new List<string[]>();
        foreach(var (table,column) in new[]{("CatalogBrand","Brand"),("CatalogType","Type")})
        {
            await using var lookup=new SqlCommand($"SELECT {column} FROM {table} ORDER BY {column}",c);
            await using var reader=await lookup.ExecuteReaderAsync();
            var names=new List<string>();while(await reader.ReadAsync())names.Add(reader.GetString(0));lookups.Add(names.ToArray());
        }
        return Json(new {items=rows,brands=lookups[0],types=lookups[1]});
    }
    static async Task<int> Id(Site site, string name)
    {
        await using var c = new SqlConnection($"Server=(localdb)\\MSSQLLocalDB;Database={site.Database};Integrated Security=True;TrustServerCertificate=True");
        await c.OpenAsync(); await using var q = new SqlCommand("SELECT Id FROM Catalog WHERE Name=@name", c); q.Parameters.AddWithValue("@name",name);
        return Convert.ToInt32(await q.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }
    async Task State(Site l, Site m, string operation, bool unchanged = false, string previous = null)
    {
        var a = await Data(l); var b = await Data(m);
        File.WriteAllText(Path.Combine(evidence, operation + "-legacy.json"), a);
        File.WriteAllText(Path.Combine(evidence, operation + "-modern.json"), b);
        observations.Add(new { operation = "SQL " + operation, legacy = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(a))), modern = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(b))) });
        if (a != b) failures.Add(operation + ": SQL state differs");
        if (unchanged && (a != previous || b != previous)) failures.Add(operation + ": invalid request wrote data");
    }
    async Task PostPair(Site l, Site m, string path, string name, string kind, Dictionary<string,string> values, bool invalid = false)
    {
        var before = await Data(l);
        var lf = await Form(l,path); var mf = await Form(m,path);
        foreach(var kv in values) { lf[kv.Key]=kv.Value; mf[kv.Key]=kv.Value; }
        var a=await Request(l,path,kind,lf,"POST"); var b=await Request(m,path,kind,mf,"POST");
        Compare(name,a,b,expectedStatus: invalid ? 200 : 302);
        if (invalid && (!a.Value.Contains("Name") || !a.Value.Contains("Price") || !b.Value.Contains("Name") || !b.Value.Contains("Price"))) failures.Add(name + ": validation errors missing");
        await State(l,m,name,invalid,before);
    }
    [TestMethod]
    public async Task RealDatabaseParity()
    {
        var legacyUrl=Environment.GetEnvironmentVariable("LEGACY_BASE_URL"); var modernUrl=Environment.GetEnvironmentVariable("MODERN_BASE_URL");
        if (string.IsNullOrEmpty(legacyUrl) || string.IsNullOrEmpty(modernUrl)) Assert.Inconclusive("Set LEGACY_BASE_URL and MODERN_BASE_URL; run Invoke-Parity.ps1 against isolated LocalDB copies.");
        evidence=Environment.GetEnvironmentVariable("PARITY_EVIDENCE") ?? TestContext.TestRunDirectory;
        Directory.CreateDirectory(evidence);
        using var l=new Site(legacyUrl,"eShopParity_Legacy"); using var m=new Site(modernUrl,"eShopParity_Modern");
        var persistence=Environment.GetEnvironmentVariable("PARITY_PHASE")=="persistence";
        try
        {
            if (persistence)
            {
                await State(l,m,"restart");
                var current=await Data(l);
                if (current!=File.ReadAllText(Path.Combine(evidence,"before-restart.json"))) failures.Add("Data changed across restart");
                if (!current.Contains("Parity Persisted")) failures.Add("Written product not persisted");
                Compare("persisted HTTP index",await Request(l,"/?pageSize=100","index"),await Request(m,"/?pageSize=100","index"),expectedStatus:200);
                return;
            }
            await State(l,m,"initial");
            foreach(var (size,index,count) in new[]{(1,0,1),(3,1,3),(10,0,10),(100,0,13),(3,99,0)})
            {
                var a=await Request(l,$"/?pageSize={size}&pageIndex={index}","index");var b=await Request(m,$"/?pageSize={size}&pageIndex={index}","index");
                Compare($"index {size}/{index}",a,b,expectedStatus:200);
                if(JsonSerializer.Deserialize<JsonElement>(a.Value).GetArrayLength()!=count || JsonSerializer.Deserialize<JsonElement>(b.Value).GetArrayLength()!=count)failures.Add($"Paging {size}/{index}: expected {count} rows");
            }
            foreach(var action in new[]{"Details","Edit","Delete"})
            {
                Compare(action+" valid",await Request(l,$"/Catalog/{action}/1",action=="Edit"?"form":"details"),await Request(m,$"/Catalog/{action}/1",action=="Edit"?"form":"details"),expectedStatus:200);
                foreach(var id in new[]{"","999999","0","-1","bad"})
                    Compare(action+" id="+id,await Request(l,$"/Catalog/{action}/{id}"),await Request(m,$"/Catalog/{action}/{id}"),expectedStatus:id is "" or "bad" ? 400 : 404);
            }
            Compare("create GET",await Request(l,"/Catalog/Create","form"),await Request(m,"/Catalog/Create","form"),expectedStatus:200);
            foreach(var token in new[]{"missing","invalid"})
            {
                var before=await Data(l); var f=Product("Rejected"); if(token=="invalid")f["__RequestVerificationToken"]="invalid";
                var a=await Request(l,"/Catalog/Create","",f,"POST"); var b=await Request(m,"/Catalog/Create","",f,"POST");
                Compare("antiforgery "+token,a,b,"platform: MVC5/Core antiforgery error status");
                if(a.Status<400 || b.Status<400)failures.Add("Antiforgery accepted "+token);
                await State(l,m,"antiforgery-"+token,true,before);
            }
            var bad=Product(""); bad["Price"]="1000001";
            await PostPair(l,m,"/Catalog/Create","create-invalid","validation",bad,true);
            await PostPair(l,m,"/Catalog/Create","create-valid","",Product("Parity Created"));
            var lid=await Id(l,"Parity Created"); var mid=await Id(m,"Parity Created");
            observations.Add(new {operation="generated IDs",legacy=lid,modern=mid});
            var beforeInvalidEdit=await Data(l);
            foreach(var site in new[]{l,m})
            {
                var id=site==l?lid:mid;
                var f=await Form(site,$"/Catalog/Edit/{id}"); foreach(var kv in Product("",id))f[kv.Key]=kv.Value; f["Price"]="-1";
                var result=await Request(site,$"/Catalog/Edit/{id}","validation",f,"POST");
                observations.Add(new {operation="edit invalid "+site.Database,result});
                if(result.Status!=200 || !result.Value.Contains("Name") || !result.Value.Contains("Price"))failures.Add("Edit validation missing "+site.Database);
            }
            await State(l,m,"edit-invalid",true,beforeInvalidEdit);
            Result[] edits=new Result[2]; int i=0;
            foreach(var site in new[]{l,m}) {var id=site==l?lid:mid;var f=await Form(site,$"/Catalog/Edit/{id}");foreach(var kv in Product("Parity Persisted",id))f[kv.Key]=kv.Value;edits[i++]=await Request(site,$"/Catalog/Edit/{id}","",f,"POST");}
            Compare("edit valid",edits[0],edits[1],expectedStatus:302); await State(l,m,"edit-valid");
            await PostPair(l,m,"/Catalog/Create","delete-fixture","",Product("Parity Delete"));
            Result[] deletes=new Result[2]; i=0;
            foreach(var site in new[]{l,m}) {var id=await Id(site,"Parity Delete");var f=await Form(site,$"/Catalog/Delete/{id}");f["id"]=id.ToString();deletes[i++]=await Request(site,$"/Catalog/Delete/{id}","",f,"POST");}
            Compare("delete POST",deletes[0],deletes[1],expectedStatus:302);await State(l,m,"delete");
            foreach(var id in new[]{1,2,12,999999,0})
                Compare("picture "+id,await Request(l,$"/items/{id}/pic","image"),await Request(m,$"/items/{id}/pic","image"),expectedStatus:id==0?400:id==999999?404:200);
            Compare("picture missing ID",await Request(l,"/items/pic"),await Request(m,"/items/pic"),expectedStatus:404);
            foreach(var path in new[]{"/api/brands","/api/brands/1","/api/brands/999999"})
                Compare(path,await Request(l,path,"json"),await Request(m,path,"json"),expectedStatus:path.EndsWith("999999")?404:200);
            foreach(var id in new[]{1,999999})
                Compare("DELETE brand "+id,await Request(l,$"/api/brands/{id}",method:"DELETE"),await Request(m,$"/api/brands/{id}",method:"DELETE"),expectedStatus:id==1?200:404);
            Compare("/api",await Request(l,"/api","json"),await Request(m,"/api","json"),"legacy quirk: /api does not resolve to the MVC action (404); Core explicitly maps it (200)");
            Compare("/api/files",await Request(l,"/api/files","file"),await Request(m,"/api/files","file"),"platform: binary serialization replaced by JSON; binary never deserialized");
            Compare("XML brands",await Request(l,"/api/brands",accept:"application/xml"),await Request(m,"/api/brands",accept:"application/xml"),"platform: XML content negotiation removed");
            await State(l,m,"final");
            File.WriteAllText(Path.Combine(evidence,"before-restart.json"),await Data(l));
            File.WriteAllText(Path.Combine(evidence,"ids-legacy.json"),await Data(l,true));
            File.WriteAllText(Path.Combine(evidence,"ids-modern.json"),await Data(m,true));
        }
        finally
        {
            File.WriteAllText(Path.Combine(evidence,persistence?"restart-operations.json":"operations.json"),JsonSerializer.Serialize(observations,new JsonSerializerOptions{WriteIndented=true}));
            File.WriteAllLines(Path.Combine(evidence,persistence?"restart-failures.txt":"failures.txt"),failures);
            if(failures.Count>0)Assert.Fail(string.Join(Environment.NewLine,failures));
        }
    }
    public TestContext TestContext {get;set;}
}
