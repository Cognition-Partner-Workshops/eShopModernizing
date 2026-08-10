using System.Text.Json;

namespace eShop.Shared.Serialization;

/// <summary>
/// The <c>System.Text.Json</c> replacement for the legacy
/// <c>eShopLegacy.Utilities.Serializing</c> helpers, which used the legacy binary runtime
/// formatter (a remote-code-execution risk, removed from .NET 8). The shapes mirror it —
/// object in, readable stream out; stream in, object out — so call sites port one-for-one.
/// </summary>
public static class JsonSerialization
{
    /// <summary>Serializes <paramref name="value"/> to a rewound, readable UTF-8 JSON stream.</summary>
    public static Stream SerializeToStream<T>(T value, JsonSerializerOptions? options = null)
    {
        var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, value, options ?? JsonDefaults.Options);
        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }

    /// <summary>Deserializes a UTF-8 JSON stream, rewinding it first when it is seekable.</summary>
    public static T? DeserializeFromStream<T>(Stream stream, JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }

        return JsonSerializer.Deserialize<T>(stream, options ?? JsonDefaults.Options);
    }

    public static string Serialize<T>(T value, JsonSerializerOptions? options = null) =>
        JsonSerializer.Serialize(value, options ?? JsonDefaults.Options);

    public static T? Deserialize<T>(string json, JsonSerializerOptions? options = null) =>
        JsonSerializer.Deserialize<T>(json, options ?? JsonDefaults.Options);

    public static byte[] SerializeToUtf8Bytes<T>(T value, JsonSerializerOptions? options = null) =>
        JsonSerializer.SerializeToUtf8Bytes(value, options ?? JsonDefaults.Options);
}
