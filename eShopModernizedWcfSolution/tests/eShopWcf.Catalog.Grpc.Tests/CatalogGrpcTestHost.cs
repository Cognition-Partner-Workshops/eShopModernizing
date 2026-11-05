using eShop.Catalog.Grpc.Protos;
using eShopWcf.Catalog.Data;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace eShopWcf.Catalog.Grpc.Tests;

public sealed class CatalogGrpcTestHost : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly GrpcChannel _channel;
    private readonly string _connectionString;

    public CatalogGrpcTestHost() : this("Legacy")
    {
    }

    internal CatalogGrpcTestHost(string dataset)
    {
        _connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database=eShopWcf_Test_{Guid.NewGuid():N};Integrated Security=true;TrustServerCertificate=true";
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:CatalogDb", _connectionString);
            builder.UseSetting("Seed:Dataset", dataset);
        });
        _channel = GrpcChannel.ForAddress(_factory.Server.BaseAddress,
            new GrpcChannelOptions { HttpHandler = _factory.Server.CreateHandler() });
        Client = new eShop.Catalog.Grpc.Protos.Catalog.CatalogClient(_channel);
    }

    public eShop.Catalog.Grpc.Protos.Catalog.CatalogClient Client { get; }
    public IServiceProvider Services => _factory.Services;

    public CatalogDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlServer(_connectionString).Options);

    public void Dispose()
    {
        _channel.Dispose();
        _factory.Dispose();
        using var db = CreateContext();
        db.Database.EnsureDeleted();
        SqlConnection.ClearAllPools();
    }
}
