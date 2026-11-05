using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using eShopWcf.Parity;

namespace eShopWcf.Catalog.Grpc.Tests;

public sealed class ParitySoapTests
{
    [Theory]
    [InlineData("19.5000", "19.5")]
    [InlineData("12.3456", "12.3456")]
    [InlineData("-1.2500", "-1.25")]
    [InlineData("0.0000", "0")]
    public void CanonicalPriceNormalizesScaleWithoutRounding(string raw, string expected) =>
        Assert.Equal(expected, Canonical.Money(decimal.Parse(raw, System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public async Task EmptySoapListIsAnEmptyCanonicalArray()
    {
        using var handler = new SoapHandler(Response("GetCatalogItems", ""));
        using var http = new HttpClient(handler);
        var client = new SoapClient(http, new Uri("http://localhost/CatalogService.svc"));
        var outcome = await client.CallAsync("GetCatalogItems", new JsonObject { ["brandIdFilter"] = -1, ["typeIdFilter"] = 0 });
        Assert.Empty(Assert.IsType<JsonArray>(outcome.Value));
        Assert.Equal("\"http://tempuri.org/ICatalogService/GetCatalogItems\"", handler.Action);
        Assert.Contains("<brandIdFilter>-1</brandIdFilter>", handler.Request, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NilSoapResultIsJsonNullNotAnEmptyItem()
    {
        string xml = $"<s:Envelope xmlns:s='{Canonical.Soap}'><s:Body><FindCatalogItemResponse xmlns='{Canonical.Contract}'><FindCatalogItemResult xmlns:i='{Canonical.Instance}' i:nil='true'/></FindCatalogItemResponse></s:Body></s:Envelope>";
        using var handler = new SoapHandler(xml);
        using var http = new HttpClient(handler);
        var outcome = await new SoapClient(http, new Uri("http://localhost/")).CallAsync("FindCatalogItem", new JsonObject { ["id"] = 0 });
        Assert.Null(outcome.Value);
        Assert.Equal(xml, outcome.Raw);
    }

    [Fact]
    public async Task SoapFaultPreservesFaultstringAndRawBody()
    {
        string xml = $"<s:Envelope xmlns:s='{Canonical.Soap}'><s:Body><s:Fault><faultcode>s:Server</faultcode><faultstring>Failed &amp; retained</faultstring></s:Fault></s:Body></s:Envelope>";
        using var handler = new SoapHandler(xml, HttpStatusCode.InternalServerError);
        using var http = new HttpClient(handler);
        var outcome = await new SoapClient(http, new Uri("http://localhost/")).CallAsync("RemoveCatalogItem", new JsonObject());
        Assert.Equal("Failed & retained", outcome.Value!["fault"]!.GetValue<string>());
        Assert.Equal(xml, outcome.Raw);
    }

    [Fact]
    public async Task SoapWriteEscapesUnicodeXmlAndUsesAlphabeticalDataContractMembers()
    {
        using var handler = new SoapHandler($"<s:Envelope xmlns:s='{Canonical.Soap}'><s:Body><CreateCatalogItemResponse xmlns='{Canonical.Contract}'/></s:Body></s:Envelope>");
        using var http = new HttpClient(handler);
        var item = Canonical.Node(new { Id = 999, Name = (string?)null, Description = "Parity Ünicode <&>", Price = "12.3456", Picturefilename = "parity.png", CatalogBrandId = 2, CatalogTypeId = 1 });
        var outcome = await new SoapClient(http, new Uri("http://localhost/")).CallAsync("CreateCatalogItem", new JsonObject { ["item"] = item });
        Assert.True(outcome.Value!["ok"]!.GetValue<bool>());
        var payload = XDocument.Parse(handler.Request!).Descendants(Canonical.Contract + "catalogItem").Single();
        Assert.Equal("CatalogBrand,CatalogBrandId,CatalogType,CatalogTypeId,Description,Id,Name,Picturefilename,Price", string.Join(',', payload.Elements().Select(x => x.Name.LocalName)));
        Assert.Equal("Parity Ünicode <&>", payload.Element(Canonical.Models + "Description")!.Value);
        Assert.True(Canonical.Nil(payload.Element(Canonical.Models + "Name")!));
    }

    [Fact]
    public async Task NonSoapErrorIsNotMisclassifiedAsLegacyFault()
    {
        using var handler = new SoapHandler("<html>Unavailable</html>", HttpStatusCode.ServiceUnavailable);
        using var http = new HttpClient(handler);
        await Assert.ThrowsAsync<InvalidDataException>(() => new SoapClient(http, new Uri("http://localhost/")).CallAsync("GetCatalogBrands", new JsonObject()));
    }

    private static string Response(string op, string result) => $"<s:Envelope xmlns:s='{Canonical.Soap}'><s:Body><{op}Response xmlns='{Canonical.Contract}'><{op}Result>{result}</{op}Result></{op}Response></s:Body></s:Envelope>";

    private sealed class SoapHandler(string response, HttpStatusCode code = HttpStatusCode.OK) : HttpMessageHandler
    {
        internal string? Request { get; private set; }
        internal string? Action { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = await request.Content!.ReadAsStringAsync(cancellationToken);
            Action = request.Headers.GetValues("SOAPAction").Single();
            return new HttpResponseMessage(code) { Content = new StringContent(response, Encoding.UTF8, "text/xml") };
        }
    }
}
