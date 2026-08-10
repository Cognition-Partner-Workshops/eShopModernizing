using eShop.Shared.Serialization;

namespace eShop.Shared.Contracts;

/// <summary>
/// Serializer for the <c>GET /api/files</c> payload (<see cref="BrandDto"/> array), the direct
/// replacement for the legacy binary response captured in the behavioral baseline.
/// </summary>
public static class BrandDtoSerializer
{
    public static string Serialize(IReadOnlyCollection<BrandDto> brands)
    {
        ArgumentNullException.ThrowIfNull(brands);
        return JsonSerialization.Serialize(brands);
    }

    public static Stream SerializeToStream(IReadOnlyCollection<BrandDto> brands)
    {
        ArgumentNullException.ThrowIfNull(brands);
        return JsonSerialization.SerializeToStream(brands);
    }

    public static IReadOnlyList<BrandDto> Deserialize(string json) =>
        JsonSerialization.Deserialize<List<BrandDto>>(json) ?? [];

    public static IReadOnlyList<BrandDto> DeserializeFromStream(Stream stream) =>
        JsonSerialization.DeserializeFromStream<List<BrandDto>>(stream) ?? [];
}
