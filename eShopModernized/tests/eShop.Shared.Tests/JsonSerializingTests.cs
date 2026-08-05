using eShop.Shared.Serialization;

namespace eShop.Shared.Tests;

public class JsonSerializingTests
{
    /// <summary>
    /// The five brands seeded by the legacy catalog, in id order — the golden baseline payload
    /// of <c>GET /api/files</c>.
    /// </summary>
    private static BrandDTO[] BaselineBrands() =>
    [
        new() { Id = 1, Brand = "Azure" },
        new() { Id = 2, Brand = ".NET" },
        new() { Id = 3, Brand = "Visual Studio" },
        new() { Id = 4, Brand = "SQL Server" },
        new() { Id = 5, Brand = "Other" },
    ];

    [Fact]
    public void Serialize_BaselineBrands_MatchesGoldenPayload()
    {
        const string expected =
            """[{"Id":1,"Brand":"Azure"},{"Id":2,"Brand":".NET"},{"Id":3,"Brand":"Visual Studio"},{"Id":4,"Brand":"SQL Server"},{"Id":5,"Brand":"Other"}]""";

        Assert.Equal(expected, JsonSerializing.Serialize(BaselineBrands()));
    }

    [Fact]
    public void Serialize_KeepsPascalCasePropertyNames()
    {
        var json = JsonSerializing.Serialize(new BrandDTO { Id = 1, Brand = "Azure" });

        Assert.Equal("""{"Id":1,"Brand":"Azure"}""", json);
        Assert.DoesNotContain("\"id\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"brand\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeDeserialize_String_RoundTrips()
    {
        var round = JsonSerializing.Deserialize<BrandDTO[]>(JsonSerializing.Serialize(BaselineBrands()));

        Assert.NotNull(round);
        Assert.Equal(BaselineBrands().Select(b => (b.Id, b.Brand)), round.Select(b => (b.Id, b.Brand)));
    }

    [Fact]
    public void SerializeToStream_Deserialize_RoundTrips()
    {
        using var stream = JsonSerializing.SerializeToStream(BaselineBrands());

        Assert.Equal(0, stream.Position);

        var round = JsonSerializing.Deserialize<BrandDTO[]>(stream);

        Assert.NotNull(round);
        Assert.Equal(BaselineBrands().Select(b => (b.Id, b.Brand)), round.Select(b => (b.Id, b.Brand)));
    }

    [Fact]
    public void Deserialize_Stream_RewindsBeforeReading()
    {
        using var stream = JsonSerializing.SerializeToStream(BaselineBrands());
        stream.Seek(0, SeekOrigin.End);

        var round = JsonSerializing.Deserialize<BrandDTO[]>(stream);

        Assert.NotNull(round);
        Assert.Equal(5, round.Length);
    }

    [Fact]
    public void SerializeToUtf8Bytes_MatchesStringPayload()
    {
        var bytes = JsonSerializing.SerializeToUtf8Bytes(BaselineBrands());

        Assert.Equal(JsonSerializing.Serialize(BaselineBrands()), System.Text.Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void Deserialize_IsCaseInsensitiveForIncomingPayloads()
    {
        var round = JsonSerializing.Deserialize<BrandDTO>("""{"id":7,"brand":"Contoso"}""");

        Assert.NotNull(round);
        Assert.Equal(7, round.Id);
        Assert.Equal("Contoso", round.Brand);
    }

    [Fact]
    public void Deserialize_NullStream_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => JsonSerializing.Deserialize<BrandDTO>((Stream)null!));
    }
}
