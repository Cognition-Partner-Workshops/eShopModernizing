using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace eShop.Catalog.Api.Tests;

/// <summary>
/// Parity tests for the ported Web API 2 surface: every row of the Behavioral Baseline section 6
/// HTTP table is asserted on status code and exact JSON body, against the mock data provider.
/// </summary>
public class CatalogApiParityTests : IClassFixture<CatalogApiFactory>
{
    private const string GoldenBrandsJson =
        """[{"Id":1,"Brand":"Azure"},{"Id":2,"Brand":".NET"},{"Id":3,"Brand":"Visual Studio"},{"Id":4,"Brand":"SQL Server"},{"Id":5,"Brand":"Other"}]""";

    private readonly CatalogApiFactory _factory;

    public CatalogApiParityTests(CatalogApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Get_brands_returns_the_seeded_brands_as_PascalCase_json()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/brands", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(GoldenBrandsJson, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(1, """{"Id":1,"Brand":"Azure"}""")]
    [InlineData(2, """{"Id":2,"Brand":".NET"}""")]
    [InlineData(5, """{"Id":5,"Brand":"Other"}""")]
    public async Task Get_brand_by_id_returns_the_brand(int id, string expectedJson)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri($"/api/brands/{id}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedJson, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_brand_by_unknown_id_returns_404()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/brands/999", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// Accepted delta (decision D-03): the legacy no-op <c>DELETE api/brands/{id}</c> is not ported,
    /// so the route exists for GET only and the verb is rejected.
    /// </summary>
    [Fact]
    public async Task Delete_brand_is_not_supported()
    {
        using var client = _factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri("/api/brands/2", UriKind.Relative));
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    /// <summary>
    /// Accepted delta (contradiction C-07): the legacy runtime binary serialization stream served
    /// as <c>text/html</c> is replaced by the same brands as JSON.
    /// </summary>
    [Fact]
    public async Task Get_files_returns_the_brand_dtos_as_json()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/files", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(GoldenBrandsJson, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_pic_returns_the_picture_of_the_catalog_item()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/items/1/pic", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);

        var contentRoot = _factory.Services.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
        var expected = await File.ReadAllBytesAsync(Path.Combine(contentRoot, "Pics", "1.png"));
        Assert.Equal(expected, await response.Content.ReadAsByteArrayAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Get_pic_returns_400_for_a_non_positive_id(int catalogItemId)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(
            new Uri(
                string.Create(CultureInfo.InvariantCulture, $"/items/{catalogItemId}/pic"),
                UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_pic_returns_404_for_an_unknown_catalog_item()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/items/999/pic", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_pic_returns_404_when_the_picture_file_is_missing()
    {
        var emptyPicsFolder = Path.Combine(Path.GetTempPath(), "eshop-pics-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(emptyPicsFolder);

        try
        {
            using var factory = new CatalogApiFactory { PicsFolder = emptyPicsFolder };
            using var client = factory.CreateClient();

            var response = await client.GetAsync(new Uri("/items/1/pic", UriKind.Relative));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        finally
        {
            Directory.Delete(emptyPicsFolder, recursive: true);
        }
    }

    /// <summary>
    /// The legacy <c>Controllers/Api/CatalogController.cs</c> route was dead (it returned 404); the
    /// controller is not ported, so the modernized app answers 404 for the same path.
    /// </summary>
    [Fact]
    public async Task Get_api_root_returns_404()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_document_is_served_in_development()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/swagger/v1/swagger.json", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/api/brands", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }
}
