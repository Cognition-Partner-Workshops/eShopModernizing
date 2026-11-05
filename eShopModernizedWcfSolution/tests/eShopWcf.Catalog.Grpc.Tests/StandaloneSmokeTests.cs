using System.Diagnostics;
using System.Net;
using eShop.Catalog.Grpc.Protos;
using eShopWcf.Catalog.Data;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;

namespace eShopWcf.Catalog.Grpc.Tests;

public sealed class StandaloneSmokeTests
{
    [Fact]
    public async Task KestrelServesGrpcAndHealthAgainstSmokeDatabase()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "eShopModernizedWcfSolution.sln")))
        {
            root = root.Parent;
        }

        Assert.NotNull(root);
        const string connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=eShopWcf_Smoke;Integrated Security=true;TrustServerCertificate=true";
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            WorkingDirectory = root.FullName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.ArgumentList.Add(Path.Combine(root.FullName, "src", "eShopWcf.Catalog.Grpc", "bin", "Release", "net8.0", "eShopWcf.Catalog.Grpc.dll"));
        start.ArgumentList.Add("--urls");
        start.ArgumentList.Add("http://localhost:15200");

        start.Environment["ConnectionString"] = connectionString;
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            using var channel = GrpcChannel.ForAddress("http://localhost:15200");
            var client = new eShop.Catalog.Grpc.Protos.Catalog.CatalogClient(channel);
            GetCatalogBrandsResponse? brands = null;
            for (int attempt = 0; attempt < 60; attempt++)
            {
                Assert.False(process.HasExited, "Standalone gRPC server exited before readiness.");
                try
                {
                    brands = await client.GetCatalogBrandsAsync(new GetCatalogBrandsRequest(), deadline: DateTime.UtcNow.AddSeconds(2));
                    break;
                }
                catch (RpcException exception) when (exception.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
                {
                    await Task.Delay(500);
                }
            }

            Assert.NotNull(brands);
            Assert.Equal(5, brands.Brands.Count);
            Assert.Equal(".NET Bot Black Hoodie", (await client.FindCatalogItemAsync(new FindCatalogItemRequest { Id = 1 })).Item.Name);
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            using var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:15200/health")
            {
                Version = HttpVersion.Version20,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            };
            using var health = await http.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, health.StatusCode);
            Assert.Equal("Healthy", await health.Content.ReadAsStringAsync());
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill();
            }

            await process.WaitForExitAsync();
            Console.WriteLine(await stdout);
            Console.WriteLine(await stderr);
            await using var db = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlServer(connectionString).Options);
            await db.Database.EnsureDeletedAsync();
        }
    }
}
