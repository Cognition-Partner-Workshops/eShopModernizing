using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace eShop.Catalog.Api.Tests;

/// <summary>
/// Behavioral Baseline section 6.1: <c>GET /api/brands</c>, <c>GET /api/brands/{id}</c>.
/// </summary>
public class BrandsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string GoldenBrandsJson =
        """[{"Id":1,"Brand":"Azure"},{"Id":2,"Brand":".NET"},{"Id":3,"Brand":"Visual Studio"},{"Id":4,"Brand":"SQL Server"},{"Id":5,"Brand":"Other"}]""";

    private readonly WebApplicationFactory<Program> _factory;

    public BrandsEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task GetBrands_ReturnsTheGoldenJsonArray()
    {
        var response = await _factory.CreateClient().GetAsync("/api/brands");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(GoldenBrandsJson, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetBrand_ReturnsTheGoldenJsonObject()
    {
        var response = await _factory.CreateClient().GetAsync("/api/brands/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("""{"Id":1,"Brand":"Azure"}""", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetBrand_UnknownId_Returns404WithAnEmptyBody()
    {
        var response = await _factory.CreateClient().GetAsync("/api/brands/99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DeleteBrand_IsNotPartOfTheModernizedSurface()
    {
        var response = await _factory.CreateClient().DeleteAsync("/api/brands/1");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }
}
