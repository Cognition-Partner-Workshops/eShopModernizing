using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace eShopWcf.Parity;

internal sealed class SoapClient(HttpClient http, Uri address)
{
    internal async Task<Outcome> CallAsync(string op, JsonObject input)
    {
        var operation = new XElement(Canonical.Contract + op);
        if (input["item"] is JsonObject item)
        {
            var payload = new XElement(Canonical.Contract + "catalogItem");
            foreach (string field in new[] { "CatalogBrand", "CatalogBrandId", "CatalogType", "CatalogTypeId", "Description", "Id", "Name", "Picturefilename", "Price" })
            {
                payload.Add(Field(Canonical.Models, field, item[field]));
            }

            operation.Add(payload);
        }
        else if (input["stock"] is JsonObject stock)
        {
            var payload = new XElement(Canonical.Contract + "catalogItemsStock");
            foreach (string field in new[] { "AvailableStock", "CatalogItemId", "Date", "StockId" })
            {
                payload.Add(Field(Canonical.Stock, field, stock[field]));
            }

            operation.Add(payload);
        }
        else
        {
            foreach (var field in input)
            {
                operation.Add(Field(Canonical.Contract, field.Key, field.Value));
            }
        }

        var envelope = new XElement(Canonical.Soap + "Envelope", new XElement(Canonical.Soap + "Body", operation));
        using var request = new HttpRequestMessage(HttpMethod.Post, address);
        request.Headers.Add("SOAPAction", $"\"http://tempuri.org/ICatalogService/{op}\"");
        request.Content = new StringContent(envelope.ToString(SaveOptions.DisableFormatting), Encoding.UTF8, "text/xml");
        using var response = await http.SendAsync(request).ConfigureAwait(false);
        string raw = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var document = XDocument.Parse(raw);
        var body = document.Root?.Element(Canonical.Soap + "Body") ?? throw new InvalidDataException("Not a SOAP 1.1 response: " + raw);
        if (body.Element(Canonical.Soap + "Fault") is { } fault)
        {
            return new Outcome(Canonical.Node(new { fault = fault.Element("faultstring")?.Value ?? throw new InvalidDataException("SOAP faultstring missing.") }), raw);
        }

        response.EnsureSuccessStatusCode();
        var wrapper = body.Element(Canonical.Contract + op + "Response") ?? throw new InvalidDataException("Missing operation response: " + raw);
        if (op.StartsWith("Create", StringComparison.Ordinal) || op.StartsWith("Update", StringComparison.Ordinal) || op.StartsWith("Remove", StringComparison.Ordinal))
        {
            return new Outcome(Canonical.Node(new { ok = true }), raw);
        }

        var result = wrapper.Element(Canonical.Contract + op + "Result") ?? throw new InvalidDataException("Missing result: " + raw);
        if (Canonical.Nil(result)) return new Outcome(null, raw);
        JsonNode value = op switch
        {
            "GetCatalogBrands" => Canonical.Array(result.Elements(Canonical.Models + "CatalogBrand").Select(Canonical.Brand)),
            "GetCatalogTypes" => Canonical.Array(result.Elements(Canonical.Models + "CatalogType").Select(Canonical.Type)),
            "GetCatalogItems" => Canonical.Array(result.Elements(Canonical.Models + "CatalogItem").Select(Canonical.Item)),
            "FindCatalogItem" => Canonical.Item(result),
            "GetDiscount" => Canonical.Discount(result),
            "GetAvailableStock" => JsonValue.Create(int.Parse(result.Value, CultureInfo.InvariantCulture))!,
            _ => throw new InvalidOperationException("Unsupported SOAP operation " + op),
        };
        return new Outcome(value, raw);
    }

    private static XElement Field(XNamespace ns, string name, JsonNode? value)
    {
        if (value is null) return new XElement(ns + name, new XAttribute(Canonical.Instance + "nil", "true"));
        if (value is not JsonValue) throw new InvalidDataException("Scalar expected.");
        string text = value.ToString();
        if (name is "date" or "day" or "Date") text = DateTime.Parse(text, CultureInfo.InvariantCulture).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        return new XElement(ns + name, text);
    }
}
