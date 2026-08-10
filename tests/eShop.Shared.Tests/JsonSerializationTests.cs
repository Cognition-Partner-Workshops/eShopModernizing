using System.Text;
using eShop.Shared.Serialization;
using Xunit;

namespace eShop.Shared.Tests;

public class JsonSerializationTests
{
    private sealed class Sample
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Optional { get; set; }
    }

    [Fact]
    public void SerializeToStream_returns_a_rewound_readable_stream()
    {
        using var stream = JsonSerialization.SerializeToStream(new Sample { Id = 7, Name = "Cup<T> White Mug", Price = 12m });

        Assert.True(stream.CanRead);
        Assert.Equal(0, stream.Position);

        using var reader = new StreamReader(stream, Encoding.UTF8);
        var json = reader.ReadToEnd();

        Assert.Equal("{\"Id\":7,\"Name\":\"Cup<T> White Mug\",\"Price\":12,\"Optional\":null}", json);
    }

    [Fact]
    public void Stream_round_trip_preserves_every_property()
    {
        var original = new Sample { Id = 3, Name = "Prism White T-Shirt", Price = 12.5m, Optional = "x" };

        using var stream = JsonSerialization.SerializeToStream(original);
        var actual = JsonSerialization.DeserializeFromStream<Sample>(stream);

        Assert.NotNull(actual);
        Assert.Equal(original.Id, actual!.Id);
        Assert.Equal(original.Name, actual.Name);
        Assert.Equal(original.Price, actual.Price);
        Assert.Equal(original.Optional, actual.Optional);
    }

    [Fact]
    public void DeserializeFromStream_rewinds_a_consumed_stream()
    {
        using var stream = JsonSerialization.SerializeToStream(new Sample { Id = 1, Name = "Azure" });
        stream.Seek(0, SeekOrigin.End);

        var actual = JsonSerialization.DeserializeFromStream<Sample>(stream);

        Assert.Equal(1, actual!.Id);
    }

    [Fact]
    public void DeserializeFromStream_rejects_a_null_stream() =>
        Assert.Throws<ArgumentNullException>(() => JsonSerialization.DeserializeFromStream<Sample>(null!));

    [Fact]
    public void String_round_trip_preserves_every_property()
    {
        var json = JsonSerialization.Serialize(new Sample { Id = 9, Name = ".NET", Price = 8.5m });
        var actual = JsonSerialization.Deserialize<Sample>(json);

        Assert.Equal(9, actual!.Id);
        Assert.Equal(".NET", actual.Name);
        Assert.Equal(8.5m, actual.Price);
    }

    [Fact]
    public void SerializeToUtf8Bytes_matches_the_string_form() =>
        Assert.Equal(
            JsonSerialization.Serialize(new Sample { Id = 2, Name = "Mug" }),
            Encoding.UTF8.GetString(JsonSerialization.SerializeToUtf8Bytes(new Sample { Id = 2, Name = "Mug" })));

    [Fact]
    public void Property_names_keep_their_declared_casing() =>
        Assert.Contains("\"Name\":", JsonSerialization.Serialize(new Sample { Id = 1, Name = "Azure" }), StringComparison.Ordinal);

    [Fact]
    public void Deserialization_is_case_insensitive_for_tolerant_clients()
    {
        var actual = JsonSerialization.Deserialize<Sample>("{\"id\":4,\"name\":\"SQL Server\"}");

        Assert.Equal(4, actual!.Id);
        Assert.Equal("SQL Server", actual.Name);
    }

    [Fact]
    public void Options_are_shared_and_stable() => Assert.Same(JsonDefaults.Options, JsonDefaults.Options);
}
