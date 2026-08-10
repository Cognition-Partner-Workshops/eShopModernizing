using System.Text.Json.Serialization;

namespace eShop.Shared.Contracts;

/// <summary>
/// The wire contract for one brand in the <c>GET /api/files</c> response. The legacy Web API 2
/// action returned a binary-formatted <c>List&lt;BrandDTO&gt;</c>; the modernized
/// endpoint (NET-67) returns the same logical payload as JSON.
/// </summary>
public sealed class BrandDto
{
    [JsonPropertyName("Id")]
    [JsonPropertyOrder(0)]
    public int Id { get; set; }

    [JsonPropertyName("Brand")]
    [JsonPropertyOrder(1)]
    public string Brand { get; set; } = string.Empty;
}
