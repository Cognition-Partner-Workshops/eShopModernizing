using eShop.Catalog.Data;
using eShop.Catalog.Data.Infrastructure;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DomainDiscountItem = eShop.Catalog.Domain.Entities.DiscountItem;
using DomainStock = eShop.Catalog.Domain.Entities.CatalogItemsStock;

namespace eShop.Catalog.Grpc.Tests;

/// <summary>
/// Hosts the real gRPC application in process on a SQLite in-memory catalog database and exposes a
/// <see cref="CatalogService.CatalogServiceClient" /> that talks to it over the test server handler.
/// SQL Server is not available on the Linux build agents.
/// </summary>
public sealed class CatalogGrpcApplication : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Filename=:memory:");

    public CatalogGrpcApplication()
    {
        _connection.Open();

        Client = new CatalogService.CatalogServiceClient(CreateChannel());
    }

    public CatalogService.CatalogServiceClient Client { get; }

    /// <summary>Opens a context on the same in-memory database the hosted service uses.</summary>
    public CatalogDbContext CreateContext()
        => new(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(_connection).Options);

    public void Seed()
    {
        using var context = CreateContext();

        context.CatalogBrands.AddRange(PreconfiguredData.GetPreconfiguredCatalogBrands());
        context.CatalogTypes.AddRange(PreconfiguredData.GetPreconfiguredCatalogTypes());
        context.SaveChanges();

        context.CatalogItems.AddRange(PreconfiguredData.GetPreconfiguredCatalogItems());
        context.SaveChanges();
    }

    public void Seed(params DomainStock[] stocks)
    {
        using var context = CreateContext();

        context.CatalogItemsStocks.AddRange(stocks);
        context.SaveChanges();
    }

    public void Seed(params DomainDiscountItem[] discounts)
    {
        using var context = CreateContext();

        context.DiscountItems.AddRange(discounts);
        context.SaveChanges();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment(Environments.Development);

        // AddCatalogData requires a connection string when Catalog:UseMockData is false; the value
        // is never used because the DbContext registration is replaced below.
        builder.UseSetting("Catalog:UseMockData", "false");
        builder.UseSetting("ConnectionStrings:Catalog", "Server=unused;Database=unused;");

        builder.ConfigureTestServices(services =>
        {
            var registrations = services
                .Where(descriptor =>
                    descriptor.ServiceType == typeof(DbContextOptions<CatalogDbContext>) ||
                    descriptor.ServiceType == typeof(DbContextOptions))
                .ToList();

            foreach (var registration in registrations)
            {
                services.Remove(registration);
            }

            services.AddDbContext<CatalogDbContext>(options => options.UseSqlite(_connection));
        });

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
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
