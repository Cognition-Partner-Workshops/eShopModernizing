using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace eShop.Catalog.Grpc.Tests;

/// <summary>
/// Hosts the real gRPC application with <c>Catalog:UseMockData=true</c> and no connection string,
/// which is how the container runs without SQL Server: <c>AddCatalogData</c> registers the
/// in-memory <c>CatalogServiceMock</c> and no <c>CatalogDbContext</c> at all.
/// </summary>
public sealed class CatalogGrpcMockApplication : WebApplicationFactory<Program>
{
    public CatalogGrpcMockApplication()
    {
        Client = new CatalogService.CatalogServiceClient(CreateChannel());
    }

    public CatalogService.CatalogServiceClient Client { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment(Environments.Development);

        builder.UseSetting("Catalog:UseMockData", "true");
    }

    private GrpcChannel CreateChannel()
    {
        var httpClient = CreateDefaultClient(new ResponseVersionHandler());

        return GrpcChannel.ForAddress(
            httpClient.BaseAddress!,
            new GrpcChannelOptions { HttpClient = httpClient });
    }

    /// <summary>
    /// TestServer answers with the HTTP version of its own pipeline; the gRPC client requires the
    /// response to carry the request's HTTP/2 version.
    /// </summary>
    private sealed class ResponseVersionHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            response.Version = request.Version;

            return response;
        }
    }
}
