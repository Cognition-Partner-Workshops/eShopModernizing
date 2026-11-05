using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace eShop.Catalog.Grpc.Tests;

public class ReflectionConfigurationTests
{
    [Theory]
    [InlineData("Development", null, true)]
    [InlineData("Production", null, false)]
    [InlineData("Production", "true", true)]
    [InlineData("Development", "false", false)]
    public void Reflection_IsMappedOnlyWhenEnabled(string environment, string? configured, bool expected)
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["EnableGrpcReflection"] = configured }));
        });
        using var client = factory.CreateClient();
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints;
        Assert.Equal(expected, endpoints.Any(endpoint =>
            endpoint.DisplayName?.Contains("ServerReflectionInfo", StringComparison.Ordinal) == true));
    }
}
