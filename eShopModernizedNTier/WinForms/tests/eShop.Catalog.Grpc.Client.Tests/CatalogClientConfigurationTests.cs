using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace eShop.Catalog.Grpc.Client.Tests;

public class CatalogClientConfigurationTests
{
    [Theory]
    [InlineData("http://catalog.example:5200", true)]
    [InlineData("http://localhost:5200", false)]
    [InlineData("file:///catalog", true)]
    [InlineData("not-a-uri", true)]
    public void RejectsRemotePlaintextAndInvalidAddresses(string address, bool allowHttp)
    {
        using var services = Configure(address, allowHttp);
        Assert.Throws<InvalidOperationException>(() => services.GetRequiredService<GrpcChannel>());
    }

    [Theory]
    [InlineData("http://localhost:5200", true)]
    [InlineData("http://127.0.0.1:5200", true)]
    [InlineData("http://[::1]:5200", true)]
    [InlineData("https://catalog.example:5201", false)]
    public void AcceptsLoopbackMockAndEncryptedRemoteAddresses(string address, bool allowHttp)
    {
        using var services = Configure(address, allowHttp);
        Assert.NotNull(services.GetRequiredService<ICatalogServiceClient>());
    }

    private static ServiceProvider Configure(string address, bool allowHttp)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["CatalogService:Address"] = address,
            ["CatalogService:AllowUnencryptedHttp2"] = allowHttp.ToString()
        }).Build();
        return new ServiceCollection().AddCatalogGrpcClient(configuration).BuildServiceProvider();
    }
}
