using System.IO;
using System.Runtime.Serialization.Json;

namespace eShopLegacy.Utilities
{
    public class Serializing
    {
        public Stream SerializeJson<T>(T input)
        {
            var stream = new MemoryStream();
            var serializer = new DataContractJsonSerializer(typeof(T));
            serializer.WriteObject(stream, input);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public T DeserializeJson<T>(Stream stream)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            stream.Seek(0, SeekOrigin.Begin);
            return (T)serializer.ReadObject(stream);
        }
    }
}
