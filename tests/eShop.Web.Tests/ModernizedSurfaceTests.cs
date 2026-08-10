using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace eShop.Web.Tests;

/// <summary>
/// The remaining acceptance criteria of the MVC port: the error view replaces
/// <c>HandleErrorAttribute</c>, the bundles became static files, and nothing in the modernized web
/// tier references <c>System.Web</c>.
/// </summary>
public class ModernizedSurfaceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ModernizedSurfaceTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task ErrorView_RendersTheLegacyMarkup()
    {
        var html = await _factory.CreateClient().GetStringAsync("/Home/Error");

        Assert.Contains("<title>Error</title>", html);
        Assert.Contains("<h1>Error.</h1>", html);
        Assert.Contains("<h2>An error occurred while processing your request.</h2>", html);
    }

    [Theory]
    [InlineData("/css/site.css", "text/css")]
    [InlineData("/js/jquery-3.3.1.min.js", "text/javascript")]
    [InlineData("/images/brand.png", "image/png")]
    public async Task StaticAssets_AreServedFromWwwrootInsteadOfBundles(string path, string contentType)
    {
        var response = await _factory.CreateClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(contentType, response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("/Default", "/Catalog/Index")]
    [InlineData("/Default/index/1/size/2", "/Catalog/Index?pageIndex=1&pageSize=2")]
    public async Task RetiredWebFormsUrls_RedirectPermanentlyToTheirMvcEquivalent(string legacy, string expected)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync(legacy);

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal(expected, response.Headers.Location?.OriginalString);
    }

    [Fact]
    public void WebTier_DoesNotReferenceSystemWeb()
    {
        var referenced = typeof(Program).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .Concat(typeof(Catalog.Data.ICatalogService).Assembly.GetReferencedAssemblies().Select(a => a.Name));

        Assert.DoesNotContain(referenced, name => name is not null && name.StartsWith("System.Web", StringComparison.Ordinal));
    }

    [Fact]
    public void WebTier_DoesNotUseSessionState()
    {
        var services = typeof(Program).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name);

        Assert.DoesNotContain("Microsoft.AspNetCore.Session", services);
        Assert.Null(Assembly.GetExecutingAssembly()
            .GetReferencedAssemblies()
            .FirstOrDefault(a => a.Name == "Microsoft.AspNetCore.Session"));
    }
}
