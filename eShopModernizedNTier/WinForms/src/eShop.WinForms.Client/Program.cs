using eShop.Catalog.Grpc.Client;
using eShop.WinForms.Client.Controllers;
using eShop.WinForms.Client.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace eShop.WinForms.Client;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection()
            .AddCatalogGrpcClient(configuration)
            .BuildServiceProvider();

        var catalogView = new CatalogView();
        var controller = new CatalogController(services.GetRequiredService<ICatalogServiceClient>(), catalogView);

        catalogView.Load += async (_, _) => await controller.LoadViewAsync();

        Application.Run(catalogView);
    }
}
