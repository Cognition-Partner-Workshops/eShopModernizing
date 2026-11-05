using eShopWinForms.Controllers;
using eShopWinForms.Grpc.Client;
using Microsoft.Extensions.Configuration;

namespace eShopWinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetDefaultFont(new System.Drawing.Font("Microsoft Sans Serif", 8.25F));
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        var options = new CatalogServiceClientOptions();
        options.Address = configuration["CatalogService:Address"] ?? options.Address;
        using var service = new CatalogServiceGrpcClient(options);
        using var view = new CatalogView();
        var controller = new CatalogController(service, view);
        controller.LoadView();
        view.ShowDialog();
    }
}
