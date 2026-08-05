using Microsoft.Extensions.Options;

namespace eShop.Shared.Configuration;

/// <summary>
/// Fails startup when the catalog database is required — that is, when
/// <see cref="CatalogOptions.UseMockData"/> is <see langword="false"/> — but no connection string
/// was supplied by configuration or by the <c>ConnectionStrings__Catalog</c> environment variable.
/// </summary>
internal sealed class CatalogConnectionOptionsValidator : IValidateOptions<CatalogConnectionOptions>
{
    private readonly IOptions<CatalogOptions> _catalogOptions;

    public CatalogConnectionOptionsValidator(IOptions<CatalogOptions> catalogOptions)
    {
        _catalogOptions = catalogOptions;
    }

    public ValidateOptionsResult Validate(string? name, CatalogConnectionOptions options)
    {
        if (_catalogOptions.Value.UseMockData)
        {
            return ValidateOptionsResult.Success;
        }

        return string.IsNullOrWhiteSpace(options.Catalog)
            ? ValidateOptionsResult.Fail(
                "No catalog connection string configured. Set 'ConnectionStrings:Catalog' " +
                "(environment variable 'ConnectionStrings__Catalog') or enable 'Catalog:UseMockData'.")
            : ValidateOptionsResult.Success;
    }
}
