using System.Text;
using eShop.Shared.Contracts;
using Xunit;

namespace eShop.Shared.Tests;

/// <summary>
/// The logical payload of the legacy binary <c>GET /api/files</c> golden output (Behavioral
/// Baseline §6.1): the five mock-data brands, in id order, each carrying only Id and Brand.
/// </summary>
public class BrandDtoContractTests
{
    private const string GoldenJson =
        "[{\"Id\":1,\"Brand\":\"Azure\"},{\"Id\":2,\"Brand\":\".NET\"},{\"Id\":3,\"Brand\":\"Visual Studio\"},{\"Id\":4,\"Brand\":\"SQL Server\"},{\"Id\":5,\"Brand\":\"Other\"}]";

    private static BrandDto[] GoldenBrands() =>
    [
        new() { Id = 1, Brand = "Azure" },
        new() { Id = 2, Brand = ".NET" },
        new() { Id = 3, Brand = "Visual Studio" },
        new() { Id = 4, Brand = "SQL Server" },
        new() { Id = 5, Brand = "Other" },
    ];

    [Fact]
    public void Serialize_matches_the_golden_payload() =>
        Assert.Equal(GoldenJson, BrandDtoSerializer.Serialize(GoldenBrands()));

    [Fact]
    public void SerializeToStream_matches_the_golden_payload()
    {
        using var stream = BrandDtoSerializer.SerializeToStream(GoldenBrands());
        using var reader = new StreamReader(stream, Encoding.UTF8);

        Assert.Equal(GoldenJson, reader.ReadToEnd());
    }

    [Fact]
    public void Round_trip_preserves_order_and_values()
    {
        var actual = BrandDtoSerializer.Deserialize(BrandDtoSerializer.Serialize(GoldenBrands()));

        Assert.Equal(GoldenBrands().Select(b => (b.Id, b.Brand)), actual.Select(b => (b.Id, b.Brand)));
    }

    [Fact]
    public void Stream_round_trip_preserves_order_and_values()
    {
        using var stream = BrandDtoSerializer.SerializeToStream(GoldenBrands());
        var actual = BrandDtoSerializer.DeserializeFromStream(stream);

        Assert.Equal(GoldenBrands().Select(b => (b.Id, b.Brand)), actual.Select(b => (b.Id, b.Brand)));
    }

    [Fact]
    public void Contract_carries_exactly_Id_and_Brand()
    {
        var names = typeof(BrandDto).GetProperties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal);

        Assert.Equal(["Brand", "Id"], names);
    }

    [Fact]
    public void Empty_collection_serializes_to_an_empty_array() =>
        Assert.Equal("[]", BrandDtoSerializer.Serialize([]));

    [Fact]
    public void Deserialize_of_a_json_null_yields_an_empty_list() =>
        Assert.Empty(BrandDtoSerializer.Deserialize("null"));

    [Fact]
    public void Serialize_rejects_null() =>
        Assert.Throws<ArgumentNullException>(() => BrandDtoSerializer.Serialize(null!));

    [Fact]
    public void Brand_names_are_not_unicode_escaped() =>
        Assert.DoesNotContain("\\u", BrandDtoSerializer.Serialize(GoldenBrands()), StringComparison.Ordinal);
}
