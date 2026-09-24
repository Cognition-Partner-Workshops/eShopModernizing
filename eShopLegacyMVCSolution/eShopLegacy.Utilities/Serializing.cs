using System.IO;
using System.Text.Json;

namespace eShopLegacy.Utilities
{
    public class Serializing
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = false
        };

        public Stream SerializeBinary(object input)
        {
            var stream = new MemoryStream();
            JsonSerializer.Serialize(stream, input, input.GetType(), _options);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public object DeserializeBinary(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(stream, leaveOpen: true);
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<object>(json, _options);
        }
    }
}
