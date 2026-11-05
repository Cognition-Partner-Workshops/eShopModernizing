using eShop.Catalog.Grpc.Client;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Grpc.AspNetCore.Server.Model;
using Proto = eShop.Catalog.Grpc.Protos;

namespace eShop.Catalog.Grpc.Client.Tests;

/// <summary>
/// Hosts an isolated fake gRPC service over HTTP/2; this does not establish backend parity.
/// </summary>
public sealed class CatalogServiceClientTestHost : IDisposable
{
    private readonly TestServer _server;
    private readonly GrpcChannel _channel;

    public CatalogServiceClientTestHost()
    {
        _server = new TestServer(new WebHostBuilder()
            .ConfigureServices(services => { services.AddGrpc(); services.AddSingleton<FakeCatalogService>(); services.AddSingleton<IServiceMethodProvider<FakeCatalogService>, FakeCatalogMethodProvider>(); })
            .Configure(app => { app.UseRouting(); app.UseEndpoints(endpoints => endpoints.MapGrpcService<FakeCatalogService>()); }));

        _channel = GrpcChannel.ForAddress(
            "http://localhost",
            new GrpcChannelOptions { HttpHandler = _server.CreateHandler() });

        Client = new CatalogServiceGrpcClient(new Proto.Catalog.CatalogClient(_channel));
    }

    public ICatalogServiceClient Client { get; }

    public void Dispose()
    {
        _channel.Dispose();
        _server.Dispose();
    }
}
