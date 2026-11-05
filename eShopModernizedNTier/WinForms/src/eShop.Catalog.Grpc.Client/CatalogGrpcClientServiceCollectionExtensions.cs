using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Proto = eShop.Catalog.Grpc.Protos;

namespace eShop.Catalog.Grpc.Client;

public static class CatalogGrpcClientServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ICatalogServiceClient"/> talking to the gRPC catalog service at the
    /// address configured in the <c>CatalogService</c> section (overridable with the
    /// <c>CatalogService__Address</c> environment variable).
    /// </summary>
    public static IServiceCollection AddCatalogGrpcClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CatalogServiceClientOptions>(
            configuration.GetSection(CatalogServiceClientOptions.SectionName));

        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<CatalogServiceClientOptions>>().Value;

            if (!Uri.TryCreate(options.Address, UriKind.Absolute, out var address) ||
                (address.Scheme != Uri.UriSchemeHttps && address.Scheme != Uri.UriSchemeHttp))
            {
                throw new InvalidOperationException("CatalogService:Address must be an absolute HTTP or HTTPS URI.");
            }

            if (address.Scheme == Uri.UriSchemeHttp && (!address.IsLoopback || !options.AllowUnencryptedHttp2))
            {
                throw new InvalidOperationException("CatalogService requires HTTPS except for explicitly enabled loopback mock traffic.");
            }

            return GrpcChannel.ForAddress(address);
        });

        services.AddSingleton(provider => new Proto.Catalog.CatalogClient(provider.GetRequiredService<GrpcChannel>()));
        services.AddSingleton<ICatalogServiceClient, CatalogServiceGrpcClient>();

        return services;
    }
}
