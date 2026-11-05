using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using eShop.Catalog.Grpc.Protos;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("eShopWcf.Catalog.Grpc.Tests")]

namespace eShopWcf.Parity;

internal static class Canonical
{
    internal static readonly XNamespace Contract = "http://tempuri.org/";
    internal static readonly XNamespace Models = "http://schemas.datacontract.org/2004/07/eShopWCFService.Models";
    internal static readonly XNamespace Stock = "http://schemas.datacontract.org/2004/07/eShopWCFService";
    internal static readonly XNamespace Soap = "http://schemas.xmlsoap.org/soap/envelope/";
    internal static readonly XNamespace Instance = "http://www.w3.org/2001/XMLSchema-instance";
    internal static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    internal static string Money(decimal value) => value.ToString("G29", CultureInfo.InvariantCulture);
    internal static string Day(DateTime value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    internal static JsonNode? Node(object? value) => JsonSerializer.SerializeToNode(value);
    internal static bool Nil(XElement value) => (string?)value.Attribute(Instance + "nil") == "true";
    internal static JsonNode Brand(CatalogBrand value) => Node(new { Id = value.Id, Brand = value.Brand })!;
    internal static JsonNode Type(CatalogType value) => Node(new { Id = value.Id, Type = value.Type })!;
    internal static JsonNode Brand(XElement value) => Node(new { Id = Integer(value, "Id"), Brand = Text(value, "Brand") })!;
    internal static JsonNode Type(XElement value) => Node(new { Id = Integer(value, "Id"), Type = Text(value, "Type") })!;
    internal static JsonNode Item(CatalogItem value) => Node(new
    {
        Id = value.Id,
        Description = value.Description,
        Name = value.Name,
        Price = Money(value.Price.Units + (value.Price.Nanos / 1_000_000_000M)),
        Picturefilename = value.PictureFilename,
        CatalogBrandId = value.CatalogBrandId,
        CatalogTypeId = value.CatalogTypeId,
        CatalogBrand = value.CatalogBrand is null ? null : Brand(value.CatalogBrand),
        CatalogType = value.CatalogType is null ? null : Type(value.CatalogType),
    })!;

    internal static JsonNode Item(XElement value) => Node(new
    {
        Id = Integer(value, "Id"),
        Description = Text(value, "Description"),
        Name = Text(value, "Name"),
        Price = Money(decimal.Parse(Required(value, "Price").Value, CultureInfo.InvariantCulture)),
        Picturefilename = Text(value, "Picturefilename"),
        CatalogBrandId = Integer(value, "CatalogBrandId"),
        CatalogTypeId = Integer(value, "CatalogTypeId"),
        CatalogBrand = Navigation(value, "CatalogBrand", Brand),
        CatalogType = Navigation(value, "CatalogType", Type),
    })!;

    internal static JsonNode Discount(DiscountItem value) => Node(new
    {
        Id = value.Id,
        Size = value.Size.ToString("R", CultureInfo.InvariantCulture),
        Start = Day(value.Start.ToDateTime()),
        End = Day(value.End.ToDateTime()),
    })!;

    internal static JsonNode Discount(XElement value) => Node(new
    {
        Id = Integer(value, "Id"),
        Size = double.Parse(Required(value, "Size").Value, CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture),
        Start = Day(DateTime.Parse(Required(value, "Start").Value, CultureInfo.InvariantCulture)),
        End = Day(DateTime.Parse(Required(value, "End").Value, CultureInfo.InvariantCulture)),
    })!;

    internal static JsonArray Array(IEnumerable<JsonNode> values) => new([.. values.OrderBy(x => x["Id"]!.GetValue<int>())]);
    internal static XElement Required(XElement value, string name) => value.Element(Models + name)
        ?? throw new InvalidDataException($"Missing DataContract member {name} in {value}");
    private static int Integer(XElement value, string name) => int.Parse(Required(value, name).Value, CultureInfo.InvariantCulture);
    private static string? Text(XElement value, string name)
    {
        var field = Required(value, name);
        return Nil(field) ? null : field.Value;
    }

    private static JsonNode? Navigation(XElement value, string name, Func<XElement, JsonNode> convert)
    {
        var field = Required(value, name);
        return Nil(field) ? null : convert(field);
    }
}

internal sealed record Outcome(JsonNode? Value, string Raw);
internal sealed record Step(string Op, JsonNode Input, JsonNode? Legacy, JsonNode? Modern, string Verdict, string? Mapping, string LegacyRaw, string ModernRaw);
internal sealed record WrittenItem(string Label, int LegacyId, int ModernId);
internal sealed class RunState
{
    public List<WrittenItem> Written { get; set; } = [];
    public int? RetainedLegacyId { get; set; }
    public JsonNode? RetainedLegacyItem { get; set; }
    public JsonArray? LegacyItems { get; set; }
    public JsonArray? ModernItems { get; set; }
}
