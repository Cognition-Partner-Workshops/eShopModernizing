using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace eShopPorted.Tests
{
    public class CatalogIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public CatalogIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CatalogIndex_ReturnsSuccessStatusCode()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CatalogIndex_ContainsCatalogContent()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/");
            var content = await response.Content.ReadAsStringAsync();

            Assert.Contains("Catalog manager", content);
        }

        [Fact]
        public async Task CatalogCreate_ReturnsSuccessStatusCode()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/Catalog/Create");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CatalogDetails_ReturnsSuccessForValidId()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/Catalog/Details/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ApiBrands_ReturnsSuccessStatusCode()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/Brands");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
