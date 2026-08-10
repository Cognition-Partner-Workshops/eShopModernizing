using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace eShop.Catalog.Api.Tests;

public class OpenApiDocumentTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OpenApiDocumentTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task SwaggerDocument_DescribesEveryPortedEndpoint()
    {
        var response = await _factory.CreateClient().GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/brands", out _));
        Assert.True(paths.TryGetProperty("/api/brands/{id}", out _));
        Assert.True(paths.TryGetProperty("/api/files", out _));
        Assert.True(paths.TryGetProperty("/items/{catalogItemId}/pic", out _));
    }
}
