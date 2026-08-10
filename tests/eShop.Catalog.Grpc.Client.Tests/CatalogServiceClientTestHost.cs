using eShop.Catalog.Grpc.Client;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using Proto = eShop.Catalog.Grpc.Protos;

namespace eShop.Catalog.Grpc.Client.Tests;

/// <summary>
/// Hosts the real gRPC catalog service in-process in mock-data mode (no database) and points the
/// production client wrapper at it over a real gRPC channel, so the tests cover the whole path the
/// WinForms client takes: wrapper → protobuf → HTTP/2 → service → data layer.
/// </summary>
public sealed class CatalogServiceClientTestHost : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly GrpcChannel _channel;

    public CatalogServiceClientTestHost()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("Catalog:UseMockData", "true"));

        _channel = GrpcChannel.ForAddress(
            _factory.Server.BaseAddress,
            new GrpcChannelOptions { HttpHandler = _factory.Server.CreateHandler() });

        Client = new CatalogServiceGrpcClient(new Proto.Catalog.CatalogClient(_channel));
    }

    public ICatalogServiceClient Client { get; }

    public void Dispose()
    {
        _channel.Dispose();
        _factory.Dispose();
    }
}
