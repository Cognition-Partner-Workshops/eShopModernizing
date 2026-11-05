namespace eShopWinForms.Grpc.Client;

public sealed class CatalogServiceClientOptions
{
    public string Address { get; set; } = "http://localhost:5200";
    public TimeSpan CallTimeout { get; set; } = TimeSpan.FromMinutes(1);
}
