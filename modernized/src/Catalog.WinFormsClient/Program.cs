using Catalog.Client;
using eShopWinForms.Controllers;
using System;
using System.Windows.Forms;

namespace eShopWinForms
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Address of the modernized Catalog gRPC service (the WCF endpoint
            // replacement). Overridable via the CATALOG_SERVICE_ADDRESS env var.
            var options = new CatalogGrpcClientOptions
            {
                Address = Environment.GetEnvironmentVariable("CATALOG_SERVICE_ADDRESS")
                    ?? new CatalogGrpcClientOptions().Address,
            };

            CatalogView catalogView = new CatalogView();
            using CatalogGrpcClient service = new CatalogGrpcClient(options);
            CatalogController catalogController = new CatalogController(service, catalogView);

            catalogController.LoadView();
            catalogView.ShowDialog();
        }
    }
}
