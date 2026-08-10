using System.Net;
using eShop.Catalog.Data;
using eShop.Catalog.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace eShop.Catalog.Api.Tests;

/// <summary>
/// Behavioral Baseline section 6.1: <c>GET /items/{catalogItemId:int}/pic</c> — 200 with the
/// picture's content type, 400 for a non-positive id, 404 for a missing item or file, and the
/// <c>application/octet-stream</c> fallback for unknown extensions.
/// </summary>
public class PicEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PicEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task GetPic_ServesThePngWithItsContentType()
    {
        var response = await _factory.CreateClient().GetAsync("/items/1/pic");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(151_640, (await response.Content.ReadAsByteArrayAsync()).Length);
    }

    [Theory]
    [InlineData("/items/0/pic")]
    public async Task GetPic_NonPositiveId_Returns400WithAnEmptyBody(string path)
    {
        var response = await _factory.CreateClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetPic_UnknownItem_Returns404()
    {
        var response = await _factory.CreateClient().GetAsync("/items/999/pic");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPic_MissingFile_Returns404()
    {
        using var pictures = new TemporaryPicturesFolder();
        using var factory = CreateFactory(pictures, new CatalogItem { Id = 1, PictureFileName = "1.png" });

        var response = await factory.CreateClient().GetAsync("/items/1/pic");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPic_UnknownExtension_FallsBackToOctetStream()
    {
        using var pictures = new TemporaryPicturesFolder();
        pictures.Write("1.unknown", [1, 2, 3, 4]);
        using var factory = CreateFactory(pictures, new CatalogItem { Id = 1, PictureFileName = "1.unknown" });

        var response = await factory.CreateClient().GetAsync("/items/1/pic");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/octet-stream", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, await response.Content.ReadAsByteArrayAsync());
    }

    private static WebApplicationFactory<Program> CreateFactory(TemporaryPicturesFolder pictures, CatalogItem item) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Pictures:RootPath"] = pictures.Path }));

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ICatalogService>();
                services.AddSingleton<ICatalogService>(new SingleItemCatalogService(item));
            });
        });

    private sealed class TemporaryPicturesFolder : IDisposable
    {
        public TemporaryPicturesFolder()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"eshop-pics-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Write(string fileName, byte[] content) =>
            File.WriteAllBytes(System.IO.Path.Combine(Path, fileName), content);

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }

    private sealed class SingleItemCatalogService : ICatalogService
    {
        private readonly CatalogItem _item;

        public SingleItemCatalogService(CatalogItem item) => _item = item;

        public CatalogItem? FindCatalogItem(int id) => id == _item.Id ? _item : null;

        public IEnumerable<CatalogBrand> GetCatalogBrands() => [];

        public IEnumerable<CatalogType> GetCatalogTypes() => [];

        public IReadOnlyList<CatalogItem> GetCatalogItems(int brandIdFilter, int typeIdFilter) => [_item];

        public PaginatedItems<CatalogItem> GetCatalogItemsPaginated(int pageSize, int pageIndex) =>
            new(pageIndex, pageSize, 1, [_item]);

        public void CreateCatalogItem(CatalogItem catalogItem) => throw new NotSupportedException();

        public void UpdateCatalogItem(CatalogItem catalogItem) => throw new NotSupportedException();

        public void RemoveCatalogItem(CatalogItem catalogItem) => throw new NotSupportedException();
    }
}
