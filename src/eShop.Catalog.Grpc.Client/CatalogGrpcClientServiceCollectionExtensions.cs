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

            if (options.AllowUnencryptedHttp2)
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            }

            return GrpcChannel.ForAddress(options.Address);
        });

        services.AddSingleton(provider => new Proto.Catalog.CatalogClient(provider.GetRequiredService<GrpcChannel>()));
        services.AddSingleton<ICatalogServiceClient, CatalogServiceGrpcClient>();

        return services;
    }
}
