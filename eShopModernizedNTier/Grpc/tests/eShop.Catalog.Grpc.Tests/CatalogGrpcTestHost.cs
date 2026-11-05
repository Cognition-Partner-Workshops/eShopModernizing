using eShop.Catalog.Grpc.Protos;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc.Testing;

namespace eShop.Catalog.Grpc.Tests;

/// <summary>
/// Hosts the gRPC service in-process (mock-data mode, no database) and exposes a real gRPC client
/// bound to the test server, so every test exercises the full pipeline: HTTP/2 framing, protobuf
/// serialization, the service implementation and the data layer.
/// </summary>
public sealed class CatalogGrpcTestHost : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly GrpcChannel _channel;

    public CatalogGrpcTestHost()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("Catalog:UseMockData", "true"));

        _channel = GrpcChannel.ForAddress(
            _factory.Server.BaseAddress,
            new GrpcChannelOptions { HttpHandler = _factory.Server.CreateHandler() });

        Client = new Protos.Catalog.CatalogClient(_channel);
    }

    public Protos.Catalog.CatalogClient Client { get; }

    public IServiceProvider Services => _factory.Services;

    public void Dispose()
    {
        _channel.Dispose();
        _factory.Dispose();
    }
}
