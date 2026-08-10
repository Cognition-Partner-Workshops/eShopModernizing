using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace eShop.Shared.Serialization;

/// <summary>
/// The single JSON contract for the modernized estate. The legacy Web API 2 surface emitted
/// PascalCase property names (see <c>GET /api/brands</c> in the behavioral baseline), so the
/// modernized payloads keep the property names exactly as declared on the DTOs.
/// </summary>
public static class JsonDefaults
{
    public static JsonSerializerOptions Options { get; } = Create();

    public static JsonSerializerOptions Create() => new()
    {
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        NumberHandling = JsonNumberHandling.Strict,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false,
    };
}
