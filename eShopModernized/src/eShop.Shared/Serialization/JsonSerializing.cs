using System.Text.Json;
using System.Text.Json.Serialization;

namespace eShop.Shared.Serialization;

/// <summary>
/// System.Text.Json replacement for the legacy <c>eShopLegacy.Utilities.Serializing</c>
/// (<c>SerializeBinary</c>/<c>DeserializeBinary</c>), which used the banned runtime binary formatter.
/// The shared <see cref="Options"/> keep CLR property names (PascalCase) so payloads such as
/// <see cref="BrandDTO"/> match the captured legacy shape <c>{"Id":1,"Brand":"Azure"}</c>.
/// </summary>
public static class JsonSerializing
{
    /// <summary>
    /// Serializer options shared by every JSON surface of the modernized solution.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    /// <summary>Serializes <paramref name="value"/> to a JSON string.</summary>
    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    /// <summary>Serializes <paramref name="value"/> to UTF-8 encoded JSON bytes.</summary>
    public static byte[] SerializeToUtf8Bytes<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value, Options);

    /// <summary>
    /// Serializes <paramref name="value"/> into a rewound <see cref="MemoryStream"/>.
    /// Direct replacement for the legacy <c>SerializeBinary(object)</c>.
    /// </summary>
    public static Stream SerializeToStream<T>(T value)
    {
        var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, value, Options);
        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }

    /// <summary>Deserializes <paramref name="json"/> into <typeparamref name="T"/>.</summary>
    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);

    /// <summary>
    /// Deserializes <paramref name="stream"/> into <typeparamref name="T"/>, rewinding first when possible.
    /// Direct replacement for the legacy <c>DeserializeBinary(Stream)</c>.
    /// </summary>
    public static T? Deserialize<T>(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }

        return JsonSerializer.Deserialize<T>(stream, Options);
    }

    private static JsonSerializerOptions CreateOptions() => new()
    {
        // null keeps the CLR property names verbatim: the golden baseline is PascalCase.
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = false,
    };
}
