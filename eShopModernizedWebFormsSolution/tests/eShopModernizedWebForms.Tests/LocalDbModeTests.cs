using eShopModernizedWebForms.Models;
using eShopModernizedWebForms.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace eShopModernizedWebForms.Tests
{
    // Runs the port against a real, freshly created LocalDB database (dropped afterwards).
    public sealed class LocalDbModeTests : IDisposable
    {
        private readonly string databaseName = "eShopWebFormsPortTests_" + Guid.NewGuid().ToString("N");
        private readonly string webRoot = Path.Combine(Path.GetTempPath(), "eshop-webforms-wwwroot-" + Guid.NewGuid().ToString("N"));

        private string ConnectionString =>
            Environment.GetEnvironmentVariable("WEBFORMS_TEST_SQL")?.Replace("{db}", databaseName, StringComparison.Ordinal)
            ?? $@"Data Source=(localdb)\MSSQLLocalDB; Initial Catalog={databaseName}; Integrated Security=True; MultipleActiveResultSets=True; TrustServerCertificate=True";

        private WebFormsAppFactory CreateApp(bool useCustomizationData = false) => new(new Dictionary<string, string>
        {
            ["UseMockData"] = "false",
            ["UseCustomizationData"] = useCustomizationData ? "true" : "false",
            ["ConnectionStrings:CatalogDBContext"] = ConnectionString,
            ["webroot"] = webRoot,
        });

        public LocalDbModeTests()
        {
            Directory.CreateDirectory(Path.Combine(webRoot, "Pics"));
        }

        [Fact]
        public async Task PreconfiguredSeedCrudAndRestartPersistence()
        {
            int createdId;
            using (var app = CreateApp())
            {
                var client = app.CreateBrowser();
                var home = await client.GetStringAsync(new Uri("/", UriKind.Relative));
                Assert.Contains("Showing 10 of 12 products - Page 1 - 2", home, StringComparison.Ordinal);

                FormPost.AssertRedirectToRoot(await FormPost.SubmitAsync(client, "/Catalog/Create", FormPost.ValidItem("Db Parity Cap")));
                createdId = WithService(app, s => s.GetCatalogItemsPaginated(50, 0).Data.Single(i => i.Name == "Db Parity Cap").Id);

                var edit = FormPost.ValidItem("Db Parity Cap v2");
                edit["Price"] = "1,234.50";
                edit["PictureFileName"] = "dummy.png";
                FormPost.AssertRedirectToRoot(await FormPost.SubmitAsync(client, $"/Catalog/Edit/{createdId}", edit));

                var invalid = FormPost.ValidItem("");
                var rejected = await FormPost.SubmitAsync(client, "/Catalog/Create", invalid);
                Assert.Equal(System.Net.HttpStatusCode.OK, rejected.StatusCode);
            }

            using (var restarted = CreateApp())
            {
                var client = restarted.CreateBrowser();
                var details = await client.GetStringAsync(new Uri($"/Catalog/Details/{createdId}", UriKind.Relative));
                Assert.Contains("<span>Db Parity Cap v2</span>", details, StringComparison.Ordinal);
                Assert.Contains("<span class=\"esh-price\">1234.50</span>", details, StringComparison.Ordinal);
                var home = await client.GetStringAsync(new Uri("/", UriKind.Relative));
                Assert.Contains("Showing 10 of 13 products - Page 1 - 2", home, StringComparison.Ordinal);

                FormPost.AssertRedirectToRoot(await FormPost.SubmitAsync(client, $"/Catalog/Delete/{createdId}", new Dictionary<string, string>()));
            }

            using (var restarted = CreateApp())
            {
                var missing = await restarted.CreateBrowser().GetAsync(new Uri($"/Catalog/Details/{createdId}", UriKind.Relative));
                Assert.Equal(System.Net.HttpStatusCode.NotFound, missing.StatusCode);
                Assert.Equal(12L, WithService(restarted, s => s.GetCatalogItemsPaginated(50, 0).TotalItems));
            }
        }

        [Fact]
        public async Task CustomizationModeSeedsCanonicalCsvAndExtractsPictures()
        {
            using var app = CreateApp(useCustomizationData: true);
            var home = await app.CreateBrowser().GetStringAsync(new Uri("/", UriKind.Relative));

            Assert.Equal("Showing 10 of 13 products - Page 1 - 2", System.Text.RegularExpressions.Regex.Match(home, @"Showing \d+ of \d+ products - Page \d+ - \d+").Value);
            Assert.Equal(13, Directory.GetFiles(Path.Combine(webRoot, "Pics")).Length);
            Assert.Equal(7, WithService(app, s => s.GetCatalogBrands().Count()));
            Assert.Equal(6, WithService(app, s => s.GetCatalogTypes().Count()));
        }

        [Fact]
        public async Task SchemaUsesLegacyEf6TableAndSequenceNames()
        {
            using var app = CreateApp();
            await app.CreateBrowser().GetStringAsync(new Uri("/", UriKind.Relative));

            using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand(
                "SELECT (SELECT COUNT(*) FROM sys.tables WHERE name IN ('Catalog','CatalogBrand','CatalogType')), (SELECT COUNT(*) FROM sys.sequences WHERE name IN ('catalog_hilo','catalog_brand_hilo','catalog_type_hilo'))",
                connection);
            using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal(3, reader.GetInt32(0));
            Assert.Equal(3, reader.GetInt32(1));
        }

        public void Dispose()
        {
            SqlConnection.ClearAllPools();
            var options = new DbContextOptionsBuilder<CatalogDBContext>().UseSqlServer(ConnectionString).Options;
            using (var context = new CatalogDBContext(options))
            {
                context.Database.EnsureDeleted();
            }

            Directory.Delete(webRoot, recursive: true);
        }

        private static T WithService<T>(WebFormsAppFactory app, Func<ICatalogService, T> action)
        {
            using var scope = app.Services.CreateScope();
            return action(scope.ServiceProvider.GetRequiredService<ICatalogService>());
        }
    }
}
