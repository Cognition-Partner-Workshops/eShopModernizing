using System.Net;
using eShop.Shared.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace eShop.Catalog.Api.Tests;

/// <summary>
/// Behavioral Baseline section 6.1: <c>GET /api/files</c>. The legacy payload was a
/// binary-formatted <c>List&lt;BrandDTO&gt;</c>; the modernized endpoint returns the same five
/// brands as JSON.
/// </summary>
public class FilesEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public FilesEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task GetFiles_ReturnsTheBrandDtoArrayAsJson()
    {
        var response = await _factory.CreateClient().GetAsync("/api/files");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(
            """[{"Id":1,"Brand":"Azure"},{"Id":2,"Brand":".NET"},{"Id":3,"Brand":"Visual Studio"},{"Id":4,"Brand":"SQL Server"},{"Id":5,"Brand":"Other"}]""",
            await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetFiles_RoundTripsThroughTheBrandDtoContract()
    {
        await using var stream = await _factory.CreateClient().GetStreamAsync("/api/files");

        var brands = BrandDtoSerializer.DeserializeFromStream(stream);

        Assert.Equal(5, brands.Count);
        Assert.Equal(1, brands[0].Id);
        Assert.Equal("Azure", brands[0].Brand);
    }
}
