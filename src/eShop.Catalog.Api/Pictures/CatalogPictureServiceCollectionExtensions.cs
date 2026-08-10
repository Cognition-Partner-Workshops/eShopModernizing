using Microsoft.Extensions.Options;

namespace eShop.Catalog.Api.Pictures;

public static class CatalogPictureServiceCollectionExtensions
{
    /// <summary>Binds <c>Pictures</c> and registers the picture store used by <c>PicController</c>.</summary>
    public static IServiceCollection AddCatalogPictures(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<CatalogPictureOptions>()
            .Bind(configuration.GetSection(CatalogPictureOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<CatalogPictureStore>();

        return services;
    }
}
